using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private const string LaunchAttemptHeader = "StS2 Launcher launch attempt";
    private const string AttemptUtcPrefix = "UTC:";
    private const string AttemptIdPrefix = "Attempt ID:";
    private const string AttemptPhasePrefix = "Phase:";
    private const string AttemptActionPrefix = "Action:";
    private const string AttemptSourcePrefix = "Source:";
    private const string AttemptDetailPrefix = "Detail:";
    private const string AttemptPreparedReadinessUsedPrefix = "Prepared readiness used:";
    private const string AttemptElapsedPrefix = "Launch attempt elapsed ms:";
    private const string ReadinessElapsedPrefix = "Launch readiness elapsed ms:";
    private const string ModReadinessElapsedPrefix = "Mod readiness elapsed ms:";
    private const string ReadinessEvaluationPhasePrefix = "Readiness evaluation phase:";
    private const string ReadinessCacheStatusPrefix = "Readiness cache status:";
    private const string SelectedBranchPrefix = "Selected branch:";
    private const string SelectedVersionPrefix = "Selected version:";
    private const string FilesReadyPrefix = "Files ready:";
    private const string ReadinessProblemPrefix = "Readiness problem:";
    private const string RuntimeSlotInspectedPrefix = "Runtime slot inspected:";
    private const string RuntimeSlotIdPrefix = "Runtime slot ID:";
    private const string RuntimePairingStatusPrefix = "Runtime pairing status:";
    private const string PatchCompatibilityStatusPrefix = "Patch compatibility status:";
    private const string GameDirectoryPrefix = "Game directory:";
    private const string PckPathPrefix = "PCK path:";
    private const string PckSha256Prefix = "PCK SHA256:";
    private const string SourceAssemblyPathPrefix = "Source sts2.dll path:";
    private const string SourceAssemblySha256Prefix = "Source sts2.dll SHA256:";
    private const string ActiveAndroidAssemblyPathPrefix = "Active Android sts2.dll path:";
    private const string ActiveAndroidAssemblySha256Prefix = "Active Android sts2.dll SHA256:";
    private const string RuntimePackDirectoryPrefix = "Runtime pack directory:";
    private const string RuntimePackManifestPathPrefix = "Runtime pack manifest path:";
    private const string RuntimePackUsablePrefix = "Runtime pack usable:";
    private const string RuntimePackStatusPrefix = "Runtime pack status:";
    private const string RuntimeCacheMarkerPathPrefix = "Runtime cache marker path:";
    private const string RuntimeCacheMarkerPresentPrefix = "Runtime cache marker present:";
    private const string RuntimePatchValidationMarkerPathPrefix = "Runtime patch validation marker path:";
    private const string RuntimePatchValidationMarkerPresentPrefix = "Runtime patch validation marker present:";
    private const string PatchCompatibilityMarkerPathPrefix = "Patch compatibility marker path:";
    private const string ModReadinessPhasePrefix = "Mod readiness phase:";
    private const string ModReadinessCacheStatusPrefix = "Mod readiness cache status:";
    private const string ModPlayModePrefix = "Mod play mode:";
    private const string ModInstalledCountPrefix = "Mod installed count:";
    private const string ModEnabledCountPrefix = "Mod enabled count:";
    private const string ModUnsupportedCountPrefix = "Mod unsupported count:";
    private const string SelectedModsPrefix = "Selected mods:";

    private static string LaunchAttemptPath =>
        MarkerPath(LauncherStorageNames.LaunchAttempt);

    internal static void WriteLaunchAttempt(
        string phase,
        string action,
        string source,
        string attemptId,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        bool preparedReadinessUsed,
        LauncherLaunchAttemptTiming timing,
        string detail
    )
    {
        timing ??= LauncherLaunchAttemptTiming.NotMeasured();
        var branch = readiness?.Branch ?? SafeAttemptBranch();
        TryWriteMarker(
            LaunchAttemptPath,
            new[]
            {
                LaunchAttemptHeader,
                MarkerLine(AttemptUtcPrefix, DateTime.UtcNow.ToString("O")),
                MarkerLine(AttemptIdPrefix, attemptId),
                MarkerLine(AttemptPhasePrefix, phase),
                MarkerLine(AttemptActionPrefix, action),
                MarkerLine(AttemptSourcePrefix, source),
                MarkerLine(AttemptDetailPrefix, detail),
                MarkerLine(AttemptPreparedReadinessUsedPrefix, preparedReadinessUsed),
                MarkerLine(AttemptElapsedPrefix, timing.AttemptElapsedText),
                MarkerLine(ReadinessElapsedPrefix, timing.ReadinessElapsedText),
                MarkerLine(ModReadinessElapsedPrefix, timing.ModReadinessElapsedText),
                MarkerLine(ReadinessEvaluationPhasePrefix, readiness?.EvaluationPhase),
                MarkerLine(ReadinessCacheStatusPrefix, readiness?.CacheStatus),
                MarkerLine(SelectedBranchPrefix, branch),
                MarkerLine(SelectedVersionPrefix, SteamGameBranch.DisplayName(branch)),
                MarkerLine(FilesReadyPrefix, readiness?.Ready == true),
                MarkerLine(ReadinessProblemPrefix, readiness?.ReadinessProblem),
                MarkerLine(RuntimeSlotInspectedPrefix, readiness?.HasRuntimeSlot == true),
                MarkerLine(RuntimeSlotIdPrefix, readiness?.RuntimeSlotId),
                MarkerLine(RuntimePairingStatusPrefix, readiness?.RuntimePairingStatus),
                MarkerLine(PatchCompatibilityStatusPrefix, readiness?.PatchCompatibilityStatus),
                MarkerLine(GameDirectoryPrefix, readiness?.GameDirectory),
                MarkerLine(PckPathPrefix, readiness?.PckPath),
                MarkerLine(PckSha256Prefix, readiness?.PckSha256),
                MarkerLine(SourceAssemblyPathPrefix, readiness?.SourceAssemblyPath),
                MarkerLine(SourceAssemblySha256Prefix, readiness?.SourceAssemblySha256),
                MarkerLine(ActiveAndroidAssemblyPathPrefix, readiness?.ActiveAndroidAssemblyPath),
                MarkerLine(ActiveAndroidAssemblySha256Prefix, readiness?.ActiveAndroidAssemblySha256),
                MarkerLine(RuntimePackDirectoryPrefix, readiness?.RuntimePackDirectory),
                MarkerLine(RuntimePackManifestPathPrefix, readiness?.RuntimePackManifestPath),
                MarkerLine(RuntimePackUsablePrefix, readiness?.RuntimePackUsable == true),
                MarkerLine(RuntimePackStatusPrefix, readiness?.RuntimePackStatus),
                MarkerLine(RuntimeCacheMarkerPathPrefix, readiness?.RuntimeCacheMarkerPath),
                MarkerLine(RuntimeCacheMarkerPresentPrefix, readiness?.RuntimeCacheMarkerPresent == true),
                MarkerLine(RuntimePatchValidationMarkerPathPrefix, readiness?.RuntimePatchValidationMarkerPath),
                MarkerLine(RuntimePatchValidationMarkerPresentPrefix, readiness?.RuntimePatchValidationMarkerPresent == true),
                MarkerLine(PatchCompatibilityMarkerPathPrefix, readiness?.PatchCompatibilityMarkerPath),
                MarkerLine(ModReadinessPhasePrefix, modReadiness?.Phase),
                MarkerLine(ModReadinessCacheStatusPrefix, modReadiness?.CacheStatus),
                MarkerLine(ModPlayModePrefix, modReadiness?.PlayMode),
                MarkerLine(ModInstalledCountPrefix, modReadiness?.InstalledMods ?? 0),
                MarkerLine(ModEnabledCountPrefix, modReadiness?.EnabledMods ?? 0),
                MarkerLine(ModUnsupportedCountPrefix, modReadiness?.UnsupportedMods ?? 0),
                MarkerLine(SelectedModsPrefix, modReadiness?.SelectedMods),
            }.JoinLines(),
            "Failed to write launch attempt marker"
        );
    }

    private static string MarkerLine(string prefix, string value)
        => $"{prefix} {SanitizeAttempt(value)}";

    private static string MarkerLine(string prefix, bool value)
        => MarkerLine(prefix, BoolText(value));

    private static string MarkerLine(string prefix, int value)
        => MarkerLine(prefix, value.ToString());

    private static string BoolText(bool value)
        => value ? "true" : "false";

    private static string SanitizeAttempt(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string SafeAttemptBranch()
    {
        try
        {
            return SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }
}
