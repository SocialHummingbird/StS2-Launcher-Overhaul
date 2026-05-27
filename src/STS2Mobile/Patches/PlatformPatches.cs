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
public static class PlatformPatches
{
    private static object _androidNullStrategy;

    public static void Apply(Harmony harmony)
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

        PatchPlatformUtil(harmony);

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
        PatchGetThreeLetterLanguageCode(harmony);
    }

    public static bool InitializePlatformPrefix(ref Task<bool> __result)
    {
        PatchHelper.Log("Skipping Steam initialization (mobile)");
        __result = Task.FromResult(true);
        return false;
    }

    public static bool SkipPrefix() => false;

    public static bool ReturnFalsePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Skip paths that aren't valid Godot absolute paths (must contain "://").
    public static bool CreateDirectoryPrefix(GodotFileIo __instance, string directoryPath)
    {
        var fullPath = __instance.GetFullPath(directoryPath);
        if (!fullPath.Contains("://"))
            return false;
        return true;
    }

    private static void PatchPlatformUtil(Harmony harmony)
    {
        try
        {
            var staticConstructor = typeof(PlatformUtil).TypeInitializer;
            if (staticConstructor != null)
            {
                harmony.Patch(
                    staticConstructor,
                    prefix: new HarmonyMethod(
                        typeof(PlatformPatches).GetMethod(
                            nameof(PlatformUtilStaticConstructorPrefix),
                            BindingFlags.Public | BindingFlags.Static
                        )
                    )
                );
                PatchHelper.Log("Patched PlatformUtil static constructor");
            }

            var primaryGetter = typeof(PlatformUtil).GetProperty(
                nameof(PlatformUtil.PrimaryPlatform),
                BindingFlags.Public | BindingFlags.Static
            )?.GetGetMethod();
            if (primaryGetter != null)
            {
                harmony.Patch(
                    primaryGetter,
                    prefix: new HarmonyMethod(
                        typeof(PlatformPatches).GetMethod(
                            nameof(PrimaryPlatformPrefix),
                            BindingFlags.Public | BindingFlags.Static
                        )
                    )
                );
                PatchHelper.Log("Patched PlatformUtil.PrimaryPlatform");
            }

            var getPlatformUtil = typeof(PlatformUtil).GetMethod(
                "GetPlatformUtil",
                BindingFlags.Public | BindingFlags.Static
            );
            if (getPlatformUtil != null)
            {
                harmony.Patch(
                    getPlatformUtil,
                    prefix: new HarmonyMethod(
                        typeof(PlatformPatches).GetMethod(
                            nameof(GetPlatformUtilPrefix),
                            BindingFlags.Public | BindingFlags.Static
                        )
                    )
                );
                PatchHelper.Log("Patched PlatformUtil.GetPlatformUtil");
            }

            PatchPlatformUtilMethod(harmony, "GetPlatformBranch", nameof(ReturnEmptyStringPrefix));
            PatchPlatformUtilMethod(harmony, "GetThreeLetterLanguageCode", nameof(GetThreeLetterLanguageCodePrefix));
            PatchPlatformUtilMethod(harmony, "GetRawLanguage", nameof(GetRawLanguagePrefix));
            PatchPlatformUtilMethod(harmony, "IsPlatformOverlayOpen", nameof(ReturnFalsePrefix));
            PatchPlatformUtilMethod(harmony, "SupportsInviteDialog", nameof(ReturnFalsePrefix));
            PatchPlatformUtilMethod(harmony, "OpenUrl", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "OpenVirtualKeyboard", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "CloseVirtualKeyboard", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "SetRichPresence", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "SetRichPresenceValue", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "ClearRichPresence", nameof(SkipPrefix));
            PatchPlatformUtilMethod(harmony, "GetPlayerName", nameof(GetPlayerNamePrefix));
            PatchPlatformUtilMethod(harmony, "GetLocalPlayerId", nameof(GetLocalPlayerIdPrefix));
            PatchPlatformUtilMethod(harmony, "GetFriendsWithOpenLobbies", nameof(GetFriendsWithOpenLobbiesPrefix));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil patch failed: {ex.Message}");
            throw;
        }
    }

    private static void PatchPlatformUtilMethod(Harmony harmony, string methodName, string prefixName)
    {
        var method = typeof(PlatformUtil).GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static
        );
        var prefix = typeof(PlatformPatches).GetMethod(
            prefixName,
            BindingFlags.Public | BindingFlags.Static
        );
        if (method == null || prefix == null)
        {
            PatchHelper.Log($"PlatformUtil.{methodName} patch skipped");
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log($"Patched PlatformUtil.{methodName}");
    }

    public static bool PlatformUtilStaticConstructorPrefix()
    {
        try
        {
            var strategy = GetAndroidNullStrategy();
            typeof(PlatformUtil)
                .GetField("_null", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, strategy);
            PatchHelper.Log("Skipped PlatformUtil desktop static initialization on Android");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil static constructor replacement failed: {ex.Message}");
        }

        return false;
    }

    public static bool PrimaryPlatformPrefix(ref PlatformType __result)
    {
        __result = PlatformType.None;
        return false;
    }

    public static bool GetPlatformUtilPrefix(ref object __result)
    {
        __result = GetAndroidNullStrategy();
        return false;
    }

    public static bool ReturnEmptyStringPrefix(ref string __result)
    {
        __result = string.Empty;
        return false;
    }

    public static bool GetRawLanguagePrefix(ref string __result)
    {
        __result = Godot.OS.GetLocale();
        return false;
    }

    public static bool GetPlayerNamePrefix(ref string __result)
    {
        __result = "Player";
        return false;
    }

    public static bool GetLocalPlayerIdPrefix(ref ulong __result)
    {
        __result = 0;
        return false;
    }

    public static bool GetFriendsWithOpenLobbiesPrefix(ref Task<IEnumerable<ulong>> __result)
    {
        __result = Task.FromResult<IEnumerable<ulong>>(Array.Empty<ulong>());
        return false;
    }

    private static object GetAndroidNullStrategy()
    {
        return _androidNullStrategy ??= new NullPlatformUtilStrategy();
    }

    private static void PatchGetThreeLetterLanguageCode(Harmony harmony)
    {
        try
        {
            var sts2Asm = typeof(NGame).Assembly;
            var nullStrategyType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy"
            );
            if (nullStrategyType == null)
            {
                PatchHelper.Log("Locale fix: NullPlatformUtilStrategy not found, skipping");
                return;
            }

            var method = nullStrategyType.GetMethod(
                "GetThreeLetterLanguageCode",
                BindingFlags.Public | BindingFlags.Instance
            );
            if (method == null)
            {
                PatchHelper.Log("Locale fix: GetThreeLetterLanguageCode not found, skipping");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(
                    typeof(PlatformPatches).GetMethod(
                        nameof(GetThreeLetterLanguageCodePrefix),
                        BindingFlags.Public | BindingFlags.Static
                    )
                )
            );
            PatchHelper.Log("Patched NullPlatformUtilStrategy.GetThreeLetterLanguageCode (locale fix)");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Locale fix failed: {ex.Message}");
        }
    }

    // Keep locale resolution resilient across unusual BCP-47 tags and malformed data.
    public static bool GetThreeLetterLanguageCodePrefix(ref string __result)
    {
        try
        {
            var locale = Godot.OS.GetLocale(); // e.g. "de_DE_u_mu_celsius" or "de_DE"
            foreach (var cultureName in EnumerateLocaleCandidates(locale))
            {
                if (TryResolveThreeLetterCulture(cultureName, out var threeLetter))
                {
                    __result = threeLetter;
                    PatchHelper.Log(
                        $"Locale fix: resolved '{locale}' -> '{cultureName}' -> '{__result}'"
                    );
                    return false;
                }
            }

            PatchHelper.Log("Locale fix: no locale candidates resolved, fallback to 'eng'");
            __result = "eng";
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Locale fix: fallback to 'eng' due to: {ex.Message}");
            __result = "eng";
        }
        return false;
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

    // Generates candidate locale strings from most specific to broadest fallback.
    private static IEnumerable<string> EnumerateLocaleCandidates(string locale)
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

    // Strips BCP-47 extension subtags like "-u-" (Unicode) and "-x-" (private use).
    private static string StripExtension(string locale, string extensionMarker)
    {
        var idx = locale.IndexOf(extensionMarker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? locale.Substring(0, idx) : locale;
    }
}
