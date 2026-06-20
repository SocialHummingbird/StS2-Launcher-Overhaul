using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendManualPullEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        sb.AppendLine($"Manual Pull evidence marker filename: {LauncherCloudSyncEvidence.LastManualPullMarkerFileName}");
        sb.AppendLine($"Manual Pull evidence marker path: {LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Pull evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)))}");
        sb.AppendLine($"Manual Pull evidence UTC: {LauncherCloudSyncEvidence.LastManualPullUtc(dataDir)}");
        sb.AppendLine($"Manual Pull evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPullUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Pull evidence selected branch: {LauncherCloudSyncEvidence.LastManualPullSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPullSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selector mode: {LauncherCloudSyncEvidence.LastManualPullSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version: {LauncherCloudSyncEvidence.LastManualPullSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Pull completion flag recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull completed before Push: {BoolText(LauncherCloudSyncEvidence.LastManualPullBeforePushCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull evidence is after branch switch: {BoolText(LauncherCloudSyncEvidence.LastManualPullIsAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Manual Pull evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Manual Pull completed after branch switch for selected version: {BoolText(LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(dataDir, selectedBranch))}");
    }
}
