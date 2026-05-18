using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using STS2Mobile.Launcher;
using STS2Mobile.Patches;

namespace STS2Mobile;

// Entry point for the mobile patcher. Bootstraps GodotSharp, applies all Harmony
// patches, and falls back to standalone launcher mode if game files aren't present.
public static class ModEntry
{
    private const string HarmonyId = "com.sts2mobile";
    private const int NotStarted = 0;
    private const int InProgress = 1;
    private const int Complete = 2;

    private static int _applyState = NotStarted;

    private sealed record PatchStep(string Name, Action<Harmony> Apply);

    private static readonly PatchStep[] CorePatches =
    {
        new("Model DB bootstrap", ModelDbInitPatch.Apply),
        new("Platform compatibility", PlatformPatches.Apply)
    };

    private static readonly PatchStep[] GameplayPatches =
    {
        new("Settings compatibility", SettingsPatches.Apply),
        new("UI scaling", UiScalePatches.Apply),
        new("Mobile layout", MobileLayoutPatches.Apply),
        new("Event layout", EventLayoutPatches.Apply),
        new("Merchant layout", MerchantLayoutPatches.Apply),
        new("App lifecycle", AppLifecyclePatches.Apply),
        new("Touch input", TouchInputPatches.Apply),
        new("Card reward", CardRewardPatches.Apply),
        new("Early access disclaimer", EarlyAccessDisclaimerPatches.Apply),
        new("Combat background", CombatBackgroundPatches.Apply)
    };

    private static readonly PatchStep[] OptionalPatches =
    {
        new("LAN multiplayer", LanMultiplayerPatcher.Apply),
        new("Mod loader integration", ModLoaderPatches.Apply),
        new("Launcher UI", LauncherPatches.Apply),
        new("Save diagnostics", SaveDiagnosticPatches.Apply)
    };

    // Bootstraps GodotSharp by setting up DLL import resolver, native interop,
    // and managed callbacks. Called from gd_mono.cpp before Apply().
    [UnmanagedCallersOnly]
    public static int InitializeGodotSharp(
        IntPtr godotDllHandle,
        IntPtr outManagedCallbacks,
        IntPtr unmanagedCallbacks,
        int unmanagedCallbacksSize
    )
    {
        try
        {
            DllImportResolver dllImportResolver = new GodotDllImportResolver(
                godotDllHandle
            ).OnResolveDllImport;
            var coreApiAssembly = typeof(GodotObject).Assembly;
            NativeLibrary.SetDllImportResolver(coreApiAssembly, dllImportResolver);

            NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
            ManagedCallbacks.Create(outManagedCallbacks);

            Console.Error.WriteLine("[STS2Mobile] GodotSharp bootstrapped successfully");
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[STS2Mobile] GodotSharp bootstrap failed: {e}");
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    public static void Apply()
    {
        if (Interlocked.CompareExchange(ref _applyState, InProgress, NotStarted) != NotStarted)
        {
            PatchHelper.Log("Apply already running/completed; skipping duplicate invocation.");
            return;
        }

        try
        {
            ApplyStartupPatches();
        }
        finally
        {
            _applyState = Complete;
        }
    }

    private static void ApplyStartupPatches()
    {
        PatchHelper.Log("Initializing STS2Mobile...");
        try
        {
            var harmony = new Harmony(HarmonyId);

            var required = ApplyPatchGroup("core", harmony, CorePatches, failFast: true);
            if (required.Failed > 0)
            {
                var failures = string.Join(" | ", required.Failures);
                PatchHelper.Log($"Critical startup patches failed: {failures}");
                PatchHelper.Log("Falling back to standalone launcher.");
                ScheduleStandaloneLauncher();
                return;
            }

            ApplyPatchGroup("gameplay", harmony, GameplayPatches);
            ApplyPatchGroup("optional", harmony, OptionalPatches);
            PatchHelper.Log("Startup patch orchestration complete.");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Unexpected startup error: {ex.Message}");
            ScheduleStandaloneLauncher();
        }
    }

    private static (int Applied, int Failed, IReadOnlyList<string> Failures) ApplyPatchGroup(
        string groupName,
        Harmony harmony,
        IReadOnlyList<PatchStep> steps,
        bool failFast = false
    )
    {
        var failures = new List<string>();
        var applied = 0;

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            try
            {
                step.Apply(harmony);
                applied++;
            }
            catch (Exception ex)
            {
                failures.Add($"{step.Name}: {ex.Message}");
                PatchHelper.Log($"[{groupName}] {step.Name} failed: {ex.Message}");

                if (failFast)
                    break;
            }
        }

        var failed = failures.Count;
        PatchHelper.Log($"[{groupName}] {applied}/{steps.Count} patches applied, {failed} failed");
        return (Applied: applied, Failed: failed, Failures: failures);
    }

    private static void ScheduleStandaloneLauncher()
    {
        PatchHelper.Log("Scheduling standalone launcher...");
        Callable.From(CreateStandaloneLauncher).CallDeferred();
    }

    private static void CreateStandaloneLauncher()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Callable.From(CreateStandaloneLauncher).CallDeferred();
            return;
        }

        var launcher = new LauncherUI();
        tree.Root.AddChild(launcher);
        launcher.Initialize();
        PatchHelper.Log("Standalone launcher displayed");
    }
}
