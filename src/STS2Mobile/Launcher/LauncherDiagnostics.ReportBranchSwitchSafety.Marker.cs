using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendBranchSwitchMarkerEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        var branchSwitchMarkerPresent = LauncherBranchSwitchSafety.HasMarker(dataDir);
        sb.AppendLine($"Branch switch marker filename: {LauncherBranchSwitchSafety.MarkerFileName}");
        sb.AppendLine($"Branch switch marker path: {LauncherBranchSwitchSafety.MarkerPath(dataDir)}");
        sb.AppendLine($"Branch switch marker present: {BoolText(branchSwitchMarkerPresent)}");
        sb.AppendLine($"Branch switch marker UTC: {LauncherBranchSwitchSafety.MarkerUtc(dataDir)}");
        sb.AppendLine($"Branch switch marker UTC parseable: {BoolText(LauncherBranchSwitchSafety.MarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Branch switch previous branch: {LauncherBranchSwitchSafety.PreviousBranch(dataDir)}");
        sb.AppendLine($"Branch switch selected branch: {LauncherBranchSwitchSafety.SelectedBranch(dataDir)}");
        sb.AppendLine($"Branch switch selected branch selection kind: {LauncherBranchSwitchSafety.SelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Branch switch selector mode: {LauncherBranchSwitchSafety.SelectorMode(dataDir)}");
        sb.AppendLine($"Branch switch selected version: {LauncherBranchSwitchSafety.SelectedVersion(dataDir)}");
        sb.AppendLine($"Branch switch selected version slot kind: {LauncherBranchSwitchSafety.SelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Branch switch selected version slot directory: {LauncherBranchSwitchSafety.SelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Branch switch selected branch matches current selected branch: {BoolText(LauncherBranchSwitchSafety.SelectedBranchMatches(dataDir, selectedBranch))}");
        sb.AppendLine($"Branch switch selected branch note: {LauncherBranchSwitchSafety.SelectedBranchNote(dataDir)}");
        sb.AppendLine($"Branch switch local backup forced: {BoolText(LauncherBranchSwitchSafety.LocalBackupForced(dataDir))}");
        sb.AppendLine($"Branch switch manual Push requires backup storage: {BoolText(LauncherBranchSwitchSafety.ManualPushRequiresBackupStorage(dataDir))}");
        sb.AppendLine($"Branch switch warning acknowledged: {BoolText(LauncherBranchSwitchSafety.WarningAcknowledged(dataDir))}");
        sb.AppendLine($"Branch switch non-public warning acknowledged: {BoolText(LauncherBranchSwitchSafety.NonPublicBranchWarningAcknowledged(dataDir))}");
        sb.AppendLine($"Branch switch marker has required safety evidence: {BoolText(LauncherBranchSwitchSafety.HasRequiredEvidence(dataDir))}");
        sb.AppendLine($"Branch switch marker has required safety evidence for selected branch: {BoolText(LauncherBranchSwitchSafety.HasRequiredEvidence(dataDir, selectedBranch))}");
        sb.AppendLine($"Push requires backup storage after branch switch: {BoolText(branchSwitchMarkerPresent)}");
    }
}
