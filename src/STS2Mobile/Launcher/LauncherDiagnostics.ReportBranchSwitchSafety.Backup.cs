using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendBranchSwitchBackupEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch,
        int importantSaveEvidenceCount
    )
    {
        sb.AppendLine($"Important Android local save evidence count in bounded scan: {importantSaveEvidenceCount}");
        sb.AppendLine($"Important Android local save evidence present: {BoolText(importantSaveEvidenceCount > 0)}");
        sb.AppendLine($"Backup storage permission available: {BoolText(STS2Mobile.AppPaths.HasStoragePermission())}");
        sb.AppendLine($"Backup storage directory: {STS2Mobile.AppPaths.ExternalSaveBackupsDir}");
        sb.AppendLine($"Backup storage directory exists: {BoolText(Directory.Exists(STS2Mobile.AppPaths.ExternalSaveBackupsDir))}");
        sb.AppendLine($"Branch-switch manual Push prerequisites satisfied: {BoolText(LauncherBranchSwitchSafety.ManualPushPrerequisitesSatisfied(dataDir, selectedBranch))}");
        sb.AppendLine($"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}");
        sb.AppendLine($"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}");
        sb.AppendLine($"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}");
        sb.AppendLine($"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}");
        sb.AppendLine($"Pre-Push local backup evidence after branch switch: {BoolText(LauncherBackupEvidence.HasLocalPrePushBackupAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Pre-Push cloud backup evidence after branch switch: {BoolText(LauncherBackupEvidence.HasCloudPrePushBackupAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Branch-switch pre-Push backup evidence satisfied: {BoolText(LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir))}");
    }
}
