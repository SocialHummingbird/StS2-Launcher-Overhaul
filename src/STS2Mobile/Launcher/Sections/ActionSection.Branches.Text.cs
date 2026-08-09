using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void UpdateBranchHelpText()
    {
        _branchHelpLabel.Text = _compact
            ? CompactReadyVersionHelpText()
            : SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
                + "\n"
                + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches);
        _branchHelpLabel.Visible = _branchDropdown.Visible && _branchDetailsExpanded;
        if (_branchDetailsToggle != null)
        {
            SetCompactActionButtonText(_branchDetailsToggle, _compact
                ? (_branchDetailsExpanded
                    ? CompactPlaySyncDrawerText("Hide Version", "Keep active")
                    : CompactPlaySyncDrawerText(
                        $"Change Version: {SteamGameBranch.CompactDisplayName(_gameBranch, 14)}",
                        "Version target"
                    ))
                : (_branchDetailsExpanded
                    ? "Hide Version Details"
                    : "Show Version Details"));
        }
        if (_readyVersionSummaryLabel != null)
        {
            _readyVersionSummaryLabel.Text = _compact
                ? CompactReadyVersionSummary()
                : $"Ready version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n"
                    + $"Slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}. Play uses this version.";
        }
    }
}
