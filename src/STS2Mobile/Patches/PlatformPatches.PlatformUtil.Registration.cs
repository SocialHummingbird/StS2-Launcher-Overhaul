using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;

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
        PatchPlatformUtilMethod(harmony, GetPlatformBranch, nameof(ReturnEmptyStringPrefix));
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

        if (string.Equals(methodName, GetPlatformBranch, StringComparison.Ordinal)
            && method.ReturnType != typeof(string))
        {
            PatchHelper.Log($"PlatformUtil.{methodName} patch skipped; return type is {method.ReturnType.FullName}");
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
