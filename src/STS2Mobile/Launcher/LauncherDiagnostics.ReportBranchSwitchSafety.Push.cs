using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendManualPushEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        sb.AppendLine($"Manual Push evidence marker filename: {LauncherCloudSyncEvidence.LastManualPushMarkerFileName}");
        sb.AppendLine($"Manual Push evidence marker path: {LauncherCloudSyncEvidence.LastManualPushMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Push evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPushMarkerPath(dataDir)))}");
        sb.AppendLine($"Latest manual Push evidence outcome: {LauncherCloudSyncEvidence.LatestManualPushEvidenceOutcome(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence UTC: {LauncherCloudSyncEvidence.LatestManualPushEvidenceUtc(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected branch: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedBranch(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected branch selection kind: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selector mode: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectorMode(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersion(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version slot kind: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version slot directory: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence reason: {LauncherCloudSyncEvidence.LatestManualPushEvidenceReason(dataDir)}");
        sb.AppendLine($"Manual Push evidence UTC: {LauncherCloudSyncEvidence.LastManualPushUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPushUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Push evidence selected branch: {LauncherCloudSyncEvidence.LastManualPushSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPushSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Push evidence selector mode: {LauncherCloudSyncEvidence.LastManualPushSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version: {LauncherCloudSyncEvidence.LastManualPushSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPushSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPushSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded local backup count: {LauncherCloudSyncEvidence.LastManualPushRecordedLocalBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded cloud backup count: {LauncherCloudSyncEvidence.LastManualPushRecordedCloudBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded latest local backup UTC: {LauncherCloudSyncEvidence.LastManualPushRecordedLatestLocalBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded latest cloud backup UTC: {LauncherCloudSyncEvidence.LastManualPushRecordedLatestCloudBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded important local save evidence count: {LauncherCloudSyncEvidence.LastManualPushRecordedImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded baseline prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushRecordedBaselinePrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push completion flag recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPushCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Push evidence is after branch switch: {BoolText(LauncherCloudSyncEvidence.LastManualPushIsAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Manual Push evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPushMatchesSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Manual Push evidence recorded pre-Push backup evidence satisfied: {BoolText(LauncherCloudSyncEvidence.LastManualPushPrePushBackupEvidenceSatisfied(dataDir))}");
        sb.AppendLine($"Manual Push completed after branch switch for selected version with backup evidence: {BoolText(LauncherCloudSyncEvidence.HasManualPushAfterBranchSwitch(dataDir, selectedBranch))}");
    }
}
