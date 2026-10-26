using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static void PatchPlatformUtilStaticConstructor(Harmony harmony)
    {
        var staticConstructor = typeof(PlatformUtil).TypeInitializer;
        if (staticConstructor == null)
            return;

        harmony.Patch(
            staticConstructor,
            prefix: new HarmonyMethod(Prefix(nameof(PlatformUtilStaticConstructorPrefix)))
        );
        PatchHelper.Log("Patched PlatformUtil static constructor");
    }

    private static void PatchPlatformUtilPrimaryPlatformGetter(Harmony harmony)
    {
        var primaryGetter = typeof(PlatformUtil).GetProperty(
            nameof(PlatformUtil.PrimaryPlatform),
            BindingFlags.Public | BindingFlags.Static
        )?.GetGetMethod();
        if (primaryGetter == null)
            return;

        harmony.Patch(
            primaryGetter,
            prefix: new HarmonyMethod(Prefix(nameof(PrimaryPlatformPrefix)))
        );
        PatchHelper.Log("Patched PlatformUtil.PrimaryPlatform");
    }

    private static void PatchGetPlatformUtil(Harmony harmony)
    {
        var getPlatformUtil = typeof(PlatformUtil).GetMethod(
            GetPlatformUtil,
            BindingFlags.Public | BindingFlags.Static
        );
        if (getPlatformUtil == null)
            return;

        harmony.Patch(
            getPlatformUtil,
            prefix: new HarmonyMethod(Prefix(nameof(GetPlatformUtilPrefix)))
        );
        PatchHelper.Log($"Patched PlatformUtil.{GetPlatformUtil}");
    }

    private static void PatchPlatformUtilMethods(Harmony harmony)
    {
        PatchPlatformBranch(harmony);
        PatchPlatformUtilMethod(
            harmony,
            GetThreeLetterLanguageCode,
            nameof(GetThreeLetterLanguageCodePrefix)
        );
        PatchPlatformUtilMethod(harmony, GetRawLanguage, nameof(GetRawLanguagePrefix));
        PatchPlatformUtilMethod(
            harmony,
            GetSupportedWindowMode,
            nameof(GetSupportedWindowModePrefix)
        );
        PatchPlatformUtilMethod(harmony, IsPlatformOverlayOpen, nameof(ReturnFalsePrefix));
        PatchPlatformUtilMethod(harmony, SupportsInviteDialog, nameof(ReturnFalsePrefix));
        PatchPlatformUtilMethod(harmony, OpenUrl, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, OpenVirtualKeyboard, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, CloseVirtualKeyboard, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, SetRichPresence, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, SetRichPresenceValue, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, ClearRichPresence, nameof(SkipPrefix));
        PatchPlatformUtilMethod(harmony, GetPlayerName, nameof(GetPlayerNamePrefix));
        PatchPlatformUtilMethod(harmony, GetLocalPlayerId, nameof(GetLocalPlayerIdPrefix));
        PatchPlatformUtilMethod(
            harmony,
            GetFriendsWithOpenLobbies,
            nameof(GetFriendsWithOpenLobbiesPrefix)
        );
    }

    internal static void PatchPlatformBranch(Harmony harmony)
    {
        var method = typeof(PlatformUtil).GetMethod(GetPlatformBranch,
            BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
            ?? throw new MissingMethodException(typeof(PlatformUtil).FullName, GetPlatformBranch);
        if (method.ReturnType != typeof(string)) RequirePlatformBranchConverter(method.ReturnType);
        var prefix = Prefix(nameof(GetPlatformBranchPrefix)).MakeGenericMethod(method.ReturnType);
        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log($"Patched PlatformUtil.{GetPlatformBranch} ({method.ReturnType.FullName})");
    }

    private static bool GetPlatformBranchPrefix<T>(ref T __result)
    {
        var branch = ModLoaderPatches.PreparedGameBranch ?? LauncherPreferences.ReadGameBranch();
        __result = (T)PlatformBranchValue(typeof(T), branch);
        return false;
    }

    internal static object PlatformBranchValue(Type returnType, string branch)
    {
        if (returnType == typeof(string)) return branch;
        var value = RequirePlatformBranchConverter(returnType).Invoke(null, new object[] { branch });
        if (value != null) return value;
        // Match SteamPlatformUtilStrategy for names that this game cannot map.
        PatchHelper.Log($"Selected game branch '{branch}' could not be mapped to {returnType.Name}; using None.");
        return Enum.Parse(returnType, "None");
    }

    private static MethodInfo RequirePlatformBranchConverter(Type returnType)
    {
        if (returnType.IsEnum && returnType.FullName == "MegaCrit.Sts2.Core.Platform.PlatformBranch"
            && returnType.GetField("None", BindingFlags.Public | BindingFlags.Static)?.IsLiteral == true)
        {
            var converter = returnType.Assembly.GetType("MegaCrit.Sts2.Core.Platform.PlatformBranchExtensions")
                ?.GetMethod("FromName", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (converter?.ReturnType == typeof(Nullable<>).MakeGenericType(returnType)) return converter;
        }
        throw new NotSupportedException($"Unsupported PlatformUtil.{GetPlatformBranch} return/conversion shape: {returnType.FullName}.");
    }

    private static void PatchPlatformUtilMethod(Harmony harmony, string methodName, string prefixName)
    {
        var method = typeof(PlatformUtil).GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static
        );
        var prefix = Prefix(prefixName);
        if (method == null || prefix == null)
        {
            PatchHelper.Log($"PlatformUtil.{methodName} patch skipped");
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log($"Patched PlatformUtil.{methodName}");
    }

    private static MethodInfo Prefix(string prefixName)
    {
        return typeof(PlatformPatches).GetMethod(
            prefixName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );
    }
}
