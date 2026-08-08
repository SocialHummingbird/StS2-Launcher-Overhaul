using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Patches;

// Disables desktop-only platform features that are unavailable or unnecessary on mobile:
// Steam initialization, Sentry crash reporting, system info logging, and telemetry opt-in.
internal static partial class PlatformPatches
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
}
