using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Null;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Patches;

// Disables desktop-only platform features that are unavailable or unnecessary on mobile:
// Steam initialization, Sentry crash reporting, system info logging, and telemetry opt-in.
internal static class PlatformPatches
{
    private const string ClearRichPresence = "ClearRichPresence";
    private const string CloseVirtualKeyboard = "CloseVirtualKeyboard";
    private const string GetFriendsWithOpenLobbies = "GetFriendsWithOpenLobbies";
    private const string GetLocalPlayerId = "GetLocalPlayerId";
    private const string GetPlatformBranch = "GetPlatformBranch";
    private const string GetPlatformUtil = "GetPlatformUtil";
    private const string GetPlayerName = "GetPlayerName";
    private const string GetRawLanguage = "GetRawLanguage";
    private const string GetSupportedWindowMode = "GetSupportedWindowMode";
    private const string GetThreeLetterLanguageCode = "GetThreeLetterLanguageCode";
    private const string GodotAbsolutePathMarker = "://";
    private const string IsPlatformOverlayOpen = "IsPlatformOverlayOpen";
    private const string OpenUrl = "OpenUrl";
    private const string OpenVirtualKeyboard = "OpenVirtualKeyboard";
    private const string PlayerNameFallback = "Player";
    private const string NullPlatformField = "_null";
    private const string SetRichPresence = "SetRichPresence";
    private const string SetRichPresenceValue = "SetRichPresenceValue";
    private const string SupportsInviteDialog = "SupportsInviteDialog";
    private const ulong LocalPlayerIdFallback = 0;
    private static object _nullPlatformStrategy;

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(NGame),
            "InitializePlatform",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(InitializePlatformPrefix))
        );

        PatchHelper.Patch(
            harmony,
            typeof(OsDebugInfo),
            "LogSystemInfo",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(SkipPrefix))
        );

        PatchHelper.PatchGetter(
            harmony,
            typeof(PrefsSave),
            "UploadData",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(ReturnFalsePrefix))
        );

        // NullPlatformUtilStrategy's constructor calls CreateDirectory(".") which
        // fails on Android because "." is not a valid absolute Godot path.
        PatchHelper.Patch(
            harmony,
            typeof(GodotFileIo),
            "CreateDirectory",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(CreateDirectoryPrefix))
        );

        ApplyPlatformUtilPatches(harmony);

        // Skip Sentry crash reporting. Not useful for our mobile port and the
        // Sentry GDExtension is not bundled in the Android build.
        PatchHelper.Patch(
            harmony,
            typeof(SentryService),
            "Initialize",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(SkipPrefix))
        );

        PatchHelper.Patch(
            harmony,
            typeof(SentryService),
            "AfterGameInit",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(SkipPrefix))
        );

        PatchHelper.Patch(
            harmony,
            typeof(SteamStatsManager),
            "Initialize",
            prefix: PatchHelper.Method(typeof(PlatformPatches), nameof(SkipPrefix))
        );

        // Android locale tags can include Unicode/private-use extensions that
        // some .NET runtimes fail to parse directly.
        ApplyNullPlatformLanguagePatch(harmony);
    }

    private static void ApplyNullPlatformLanguagePatch(Harmony harmony)
    {
        try
        {
            var method = FindGetThreeLetterLanguageCode();
            if (method == null)
                return;

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(Prefix(nameof(GetThreeLetterLanguageCodePrefix)))
            );
            PatchHelper.Log(
                "Patched NullPlatformUtilStrategy.GetThreeLetterLanguageCode (locale fix)"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Locale fix failed: {ex.Message}");
        }
    }

    private static void ApplyPlatformUtilPatches(Harmony harmony)
    {
        try
        {
            PatchPlatformUtilStaticConstructor(harmony);
            PatchPlatformUtilPrimaryPlatformGetter(harmony);
            PatchGetPlatformUtil(harmony);
            PatchPlatformUtilMethods(harmony);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil patch failed: {ex.Message}");
            throw;
        }
    }

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

    private static MethodInfo FindGetThreeLetterLanguageCode()
    {
        var sts2Asm = typeof(NGame).Assembly;
        var nullStrategyType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy"
        );
        if (nullStrategyType == null)
        {
            PatchHelper.Log("Locale fix: NullPlatformUtilStrategy not found, skipping");
            return null;
        }

        var method = nullStrategyType.GetMethod(
            "GetThreeLetterLanguageCode",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (method == null)
            PatchHelper.Log("Locale fix: GetThreeLetterLanguageCode not found, skipping");

        return method;
    }

    private static bool InitializePlatformPrefix(ref Task<bool> __result)
    {
        PatchHelper.Log("Skipping Steam initialization (mobile)");
        __result = Task.FromResult(true);
        return false;
    }

    private static bool SkipPrefix() => false;

    private static bool ReturnFalsePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Skip paths that aren't valid Godot absolute paths (must contain "://").
    private static bool CreateDirectoryPrefix(GodotFileIo __instance, string directoryPath)
    {
        var fullPath = __instance.GetFullPath(directoryPath);
        if (!fullPath.Contains(GodotAbsolutePathMarker))
            return false;
        return true;
    }

    private static bool PlatformUtilStaticConstructorPrefix()
    {
        try
        {
            var strategy = GetAndroidNullPlatformStrategy();
            typeof(PlatformUtil)
                .GetField(NullPlatformField, BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, strategy);
            PatchHelper.Log("Skipped PlatformUtil desktop static initialization on Android");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil static constructor replacement failed: {ex.Message}");
        }

        return false;
    }

    private static bool PrimaryPlatformPrefix(ref PlatformType __result)
    {
        __result = PlatformType.None;
        return false;
    }

    private static bool GetPlatformUtilPrefix(ref object __result)
    {
        __result = GetAndroidNullPlatformStrategy();
        return false;
    }

    private static bool ReturnEmptyStringPrefix(ref string __result)
    {
        __result = string.Empty;
        return false;
    }

    private static bool GetRawLanguagePrefix(ref string __result)
    {
        __result = Godot.OS.GetLocale();
        return false;
    }

    private static bool GetSupportedWindowModePrefix(ref SupportedWindowMode __result)
    {
        __result = SupportedWindowMode.FullscreenOnly;
        return false;
    }

    private static bool GetPlayerNamePrefix(ref string __result)
    {
        __result = PlayerNameFallback;
        return false;
    }

    private static bool GetLocalPlayerIdPrefix(ref ulong __result)
    {
        __result = LocalPlayerIdFallback;
        return false;
    }

    private static bool GetFriendsWithOpenLobbiesPrefix(ref Task<IEnumerable<ulong>> __result)
    {
        __result = Task.FromResult<IEnumerable<ulong>>(Array.Empty<ulong>());
        return false;
    }

    // Keep locale resolution resilient across unusual BCP-47 tags and malformed data.
    private static bool GetThreeLetterLanguageCodePrefix(ref string __result)
    {
        __result = ResolveThreeLetterLanguageCode();
        return false;
    }

    private static string ResolveThreeLetterLanguageCode()
    {
        try
        {
            var locale = Godot.OS.GetLocale(); // e.g. "de_DE_u_mu_celsius" or "de_DE"
            foreach (var cultureName in LocaleCandidates(locale))
            {
                if (TryResolveThreeLetterCulture(cultureName, out var threeLetter))
                {
                    PatchHelper.Log(
                        $"Locale fix: resolved '{locale}' -> '{cultureName}' -> '{threeLetter}'"
                    );
                    return threeLetter;
                }
            }

            PatchHelper.Log("Locale fix: no locale candidates resolved, fallback to 'eng'");
            return "eng";
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Locale fix: fallback to 'eng' due to: {ex.Message}");
            return "eng";
        }
    }

    private static bool TryResolveThreeLetterCulture(string locale, out string threeLetter)
    {
        threeLetter = null;
        var trimmed = locale?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        try
        {
            var culture = new CultureInfo(trimmed);
            threeLetter = culture.ThreeLetterISOLanguageName;
            return !string.IsNullOrWhiteSpace(threeLetter);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static IEnumerable<string> LocaleCandidates(string locale)
    {
        var raw = (locale ?? string.Empty).Trim().Replace('_', '-');

        if (string.IsNullOrWhiteSpace(raw))
        {
            yield return "eng";
            yield break;
        }

        var sanitized = StripExtension(raw, "-u-");
        sanitized = StripExtension(sanitized, "-x-");

        yield return sanitized;

        var firstToken = sanitized.Split('-')[0];
        if (!string.IsNullOrWhiteSpace(firstToken) && !string.Equals(firstToken, sanitized))
            yield return firstToken;

        var tokens = sanitized.Split('-');
        if (tokens.Length > 1)
            yield return tokens[0];

        yield return "eng";
    }

    private static string StripExtension(string locale, string extensionMarker)
    {
        var idx = locale.IndexOf(extensionMarker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? locale.Substring(0, idx) : locale;
    }

    private static object GetAndroidNullPlatformStrategy()
    {
        try
        {
            return _nullPlatformStrategy ??= new NullPlatformUtilStrategy();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"NullPlatformUtilStrategy unavailable on Android: {ex.Message}");
            return null;
        }
    }
}
