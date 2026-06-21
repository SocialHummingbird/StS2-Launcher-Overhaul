namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static string LastManualPushSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPushMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPushSelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), SelectedBranchSelectionKindPrefix) ?? "<none>";

    internal static string LastManualPushSelectorMode(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), SelectorModePrefix) ?? "<none>";

    internal static string LastManualPushSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), SelectedVersionPrefix) ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), SelectedVersionSlotKindPrefix) ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), SelectedVersionSlotDirectoryPrefix) ?? "<none>";

    internal static string LastManualPushRecordedLocalBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), PrePushLocalBackupEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushRecordedCloudBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), PrePushCloudBackupEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushRecordedLatestLocalBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), LatestPrePushLocalBackupUtcPrefix) ?? "<none>";

    internal static string LastManualPushRecordedLatestCloudBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), LatestPrePushCloudBackupUtcPrefix) ?? "<none>";

    internal static string LastManualPushRecordedImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), ImportantLocalSaveEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushRecordedBaselinePrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), BaselineManualPushPrerequisitesSatisfiedPrefix) ?? "<none>";

    internal static bool LastManualPushCompletionRecorded(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix);

    internal static bool LastManualPushPrePushBackupEvidenceSatisfied(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), BranchSwitchPrePushBackupEvidenceSatisfiedPrefix);
}
