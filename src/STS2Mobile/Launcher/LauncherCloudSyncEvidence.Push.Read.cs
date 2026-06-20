namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static string LastManualPushSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPushMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPushSelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected branch selection kind:") ?? "<none>";

    internal static string LastManualPushSelectorMode(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Steam branch selector mode:") ?? "<none>";

    internal static string LastManualPushSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version:") ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version slot kind:") ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version slot directory:") ?? "<none>";

    internal static string LastManualPushRecordedLocalBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Pre-Push local backup evidence count:") ?? "<none>";

    internal static string LastManualPushRecordedCloudBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Pre-Push cloud backup evidence count:") ?? "<none>";

    internal static string LastManualPushRecordedLatestLocalBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Latest pre-Push local backup UTC:") ?? "<none>";

    internal static string LastManualPushRecordedLatestCloudBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Latest pre-Push cloud backup UTC:") ?? "<none>";

    internal static string LastManualPushRecordedImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Important Android local save evidence count:") ?? "<none>";

    internal static string LastManualPushRecordedBaselinePrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Baseline manual Push prerequisites satisfied:") ?? "<none>";

    internal static bool LastManualPushCompletionRecorded(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), "Manual Push completed after branch-switch safety gates:");

    internal static bool LastManualPushPrePushBackupEvidenceSatisfied(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), "Branch-switch pre-Push backup evidence satisfied:");
}
