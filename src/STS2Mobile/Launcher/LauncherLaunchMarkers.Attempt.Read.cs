using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private static readonly string[] LaunchAttemptSummaryPrefixes =
    {
        AttemptUtcPrefix,
        AttemptIdPrefix,
        AttemptPhasePrefix,
        AttemptActionPrefix,
        AttemptSourcePrefix,
        AttemptDetailPrefix,
        AttemptPreparedReadinessUsedPrefix,
        AttemptElapsedPrefix,
        ReadinessElapsedPrefix,
        ModReadinessElapsedPrefix,
        ReadinessEvaluationPhasePrefix,
        ReadinessCacheStatusPrefix,
        SelectedBranchPrefix,
        SelectedVersionPrefix,
        FilesReadyPrefix,
        ReadinessProblemPrefix,
        RuntimeSlotInspectedPrefix,
        RuntimeSlotIdPrefix,
        RuntimePairingStatusPrefix,
        PatchCompatibilityStatusPrefix,
        GameDirectoryPrefix,
        PckPathPrefix,
        PckSha256Prefix,
        SourceAssemblyPathPrefix,
        SourceAssemblySha256Prefix,
        ActiveAndroidAssemblyPathPrefix,
        ActiveAndroidAssemblySha256Prefix,
        RuntimePackUsablePrefix,
        RuntimePackDirectoryPrefix,
        RuntimePackManifestPathPrefix,
        RuntimePackStatusPrefix,
        RuntimeCacheMarkerPathPrefix,
        RuntimeCacheMarkerPresentPrefix,
        RuntimePatchValidationMarkerPathPrefix,
        RuntimePatchValidationMarkerPresentPrefix,
        PatchCompatibilityMarkerPathPrefix,
        ModReadinessPhasePrefix,
        ModReadinessCacheStatusPrefix,
        ModPlayModePrefix,
        ModInstalledCountPrefix,
        ModEnabledCountPrefix,
        ModUnsupportedCountPrefix,
        ModdedSaveCloudPushLockedPrefix,
        SelectedModsPrefix,
    };

    internal static LaunchAttemptSummary ReadLastLaunchAttempt()
    {
        var values = LauncherMarkerFile.ReadOptionalValues(
            LaunchAttemptPath,
            LaunchAttemptSummaryPrefixes
        );
        var phase = ReadAttemptValue(values, AttemptPhasePrefix);
        if (string.IsNullOrWhiteSpace(phase))
            return LaunchAttemptSummary.Missing();

        return new LaunchAttemptSummary(
            ReadAttemptValue(values, AttemptUtcPrefix),
            ReadAttemptValue(values, AttemptIdPrefix),
            phase,
            ReadAttemptValue(values, AttemptActionPrefix),
            ReadAttemptValue(values, AttemptSourcePrefix),
            ReadAttemptValue(values, AttemptDetailPrefix),
            ReadAttemptValue(values, AttemptPreparedReadinessUsedPrefix),
            ReadAttemptValue(values, AttemptElapsedPrefix),
            ReadAttemptValue(values, ReadinessElapsedPrefix),
            ReadAttemptValue(values, ModReadinessElapsedPrefix),
            ReadAttemptValue(values, ReadinessEvaluationPhasePrefix),
            ReadAttemptValue(values, ReadinessCacheStatusPrefix),
            ReadAttemptValue(values, SelectedBranchPrefix),
            ReadAttemptValue(values, SelectedVersionPrefix),
            ReadAttemptValue(values, FilesReadyPrefix),
            ReadAttemptValue(values, ReadinessProblemPrefix),
            ReadAttemptValue(values, RuntimeSlotInspectedPrefix),
            ReadAttemptValue(values, RuntimeSlotIdPrefix),
            ReadAttemptValue(values, RuntimePairingStatusPrefix),
            ReadAttemptValue(values, PatchCompatibilityStatusPrefix),
            ReadAttemptValue(values, GameDirectoryPrefix),
            ReadAttemptValue(values, PckPathPrefix),
            ReadAttemptValue(values, PckSha256Prefix),
            ReadAttemptValue(values, SourceAssemblyPathPrefix),
            ReadAttemptValue(values, SourceAssemblySha256Prefix),
            ReadAttemptValue(values, ActiveAndroidAssemblyPathPrefix),
            ReadAttemptValue(values, ActiveAndroidAssemblySha256Prefix),
            ReadAttemptValue(values, RuntimePackUsablePrefix),
            ReadAttemptValue(values, RuntimePackDirectoryPrefix),
            ReadAttemptValue(values, RuntimePackManifestPathPrefix),
            ReadAttemptValue(values, RuntimePackStatusPrefix),
            ReadAttemptValue(values, RuntimeCacheMarkerPathPrefix),
            ReadAttemptValue(values, RuntimeCacheMarkerPresentPrefix),
            ReadAttemptValue(values, RuntimePatchValidationMarkerPathPrefix),
            ReadAttemptValue(values, RuntimePatchValidationMarkerPresentPrefix),
            ReadAttemptValue(values, PatchCompatibilityMarkerPathPrefix),
            ReadAttemptValue(values, ModReadinessPhasePrefix),
            ReadAttemptValue(values, ModReadinessCacheStatusPrefix),
            ReadAttemptValue(values, ModPlayModePrefix),
            ReadAttemptValue(values, ModInstalledCountPrefix),
            ReadAttemptValue(values, ModEnabledCountPrefix),
            ReadAttemptValue(values, ModUnsupportedCountPrefix),
            ReadAttemptValue(values, ModdedSaveCloudPushLockedPrefix),
            ReadAttemptValue(values, SelectedModsPrefix)
        );
    }

    private static string ReadAttemptValue(
        IReadOnlyDictionary<string, string> values,
        string prefix
    )
        => values != null && values.TryGetValue(prefix, out var value)
            ? value
            : null;
}
