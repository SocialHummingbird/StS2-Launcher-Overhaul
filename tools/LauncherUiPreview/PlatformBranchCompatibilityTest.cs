using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;
using STS2Mobile.Launcher;
using STS2Mobile.Patches;

namespace LauncherUiPreview;

internal static class PlatformBranchCompatibilityTest
{
    internal static void Run()
    {
        PlatformPatches.PatchPlatformBranch(new Harmony("sts2-launcher.platform-branch-fixture"));
        var original = LauncherPreferences.ReadGameBranch();
        try
        {
            foreach (var branch in new[] { "public", "public-beta" })
            {
                LauncherPreferences.SaveGameBranch(branch);
                Require(ActualBranchName() == branch,
                    $"PlatformUtil.GetPlatformBranch did not expose the selected {branch} installation.");
            }
        }
        finally { LauncherPreferences.SaveGameBranch(original); }
        try
        {
            PlatformPatches.PlatformBranchValue(typeof(int), "public");
            throw new Exception("An unsupported platform branch return shape was accepted.");
        }
        catch (NotSupportedException ex)
        {
            Require(ex.Message.Contains("Unsupported PlatformUtil.GetPlatformBranch", StringComparison.Ordinal),
                "Unsupported platform branch shapes lost their explicit failure.");
        }
        var resultType = typeof(PlatformUtil).GetMethod("GetPlatformBranch")!.ReturnType;
        if (resultType.IsEnum)
        {
            Require(Enum.GetName(resultType, PlatformPatches.PlatformBranchValue(resultType, "unmapped-fixture-branch")) == "None",
                "Unknown branch names must retain the game's own None fallback.");
        }
        GD.Print($"PLATFORM_BRANCH_SHIPPING_CONTRACT return={typeof(PlatformUtil).GetMethod("GetPlatformBranch")!.ReturnType.Name} selected=public,public-beta");
    }

    internal static void VerifyCapturedBranch(string expected)
    {
        var original = LauncherPreferences.ReadGameBranch();
        try
        {
            LauncherPreferences.SaveGameBranch("public");
            Require(ModLoaderPatches.PreparedGameBranch == expected && ActualBranchName() == expected,
                "A changed launcher preference replaced the branch captured for this initialization.");
        }
        finally { LauncherPreferences.SaveGameBranch(original); }
        GD.Print($"PLATFORM_BRANCH_CAPTURED_CONTRACT branch={expected}");
    }

    private static string ActualBranchName()
    {
        var target = typeof(PlatformUtil).GetMethod("GetPlatformBranch", BindingFlags.Public | BindingFlags.Static)!;
        var result = target.Invoke(null, null);
        if (target.ReturnType == typeof(string)) return (string)result;
        var extensions = target.ReturnType.Assembly.GetType("MegaCrit.Sts2.Core.Platform.PlatformBranchExtensions", throwOnError: true)!;
        return (string)extensions.GetMethod("ToName", new[] { target.ReturnType })!.Invoke(null, new[] { result });
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
