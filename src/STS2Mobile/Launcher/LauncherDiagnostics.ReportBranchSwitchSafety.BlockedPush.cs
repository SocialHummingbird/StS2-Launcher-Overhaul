using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendManualPushBlockedEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        sb.AppendLine($"Manual Push blocked evidence marker filename: {LauncherCloudSyncEvidence.LastManualPushBlockedMarkerFileName}");
        sb.AppendLine($"Manual Push blocked evidence marker path: {LauncherCloudSyncEvidence.LastManualPushBlockedMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPushBlockedMarkerPath(dataDir)))}");
        sb.AppendLine($"Manual Push blocked evidence UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Push blocked evidence selected branch: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selector mode: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedMatchesSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Manual Push blocked evidence recorded prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedPrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded local backup count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLocalBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded cloud backup count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedCloudBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded latest local backup UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLatestLocalBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded latest cloud backup UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLatestCloudBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded important local save evidence count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded baseline prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded pre-Push backup evidence satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence reason: {LauncherCloudSyncEvidence.LastManualPushBlockedReason(dataDir)}");
        sb.AppendLine($"Manual Push blocked before upload evidence recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedBeforeUpload(dataDir))}");
    }
}
