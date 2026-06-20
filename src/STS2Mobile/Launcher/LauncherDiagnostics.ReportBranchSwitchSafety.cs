using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendBranchSwitchSafety(StringBuilder sb, string dataDir)
    {
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var importantSaveEvidenceCount = LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir);
        AppendBranchSwitchMarkerEvidence(sb, dataDir, selectedBranch);

        AppendManualPullEvidence(sb, dataDir, selectedBranch);
        AppendCurrentLocalSaveEvidence(sb, dataDir);
        AppendSaveOriginEvidence(sb, dataDir, selectedBranch);
        AppendManualPushEvidence(sb, dataDir, selectedBranch);
        AppendManualPushBlockedEvidence(sb, dataDir, selectedBranch);
        AppendBranchSwitchBackupEvidence(
            sb,
            dataDir,
            selectedBranch,
            importantSaveEvidenceCount
        );
    }
}
