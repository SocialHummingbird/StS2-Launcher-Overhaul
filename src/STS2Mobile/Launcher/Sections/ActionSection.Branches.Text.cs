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
                + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches)
                + "\n"
                + "Version/download actions affect local game files only. Steam Cloud saves move only through Pull/Push.";
        _branchHelpLabel.Visible = _branchDropdown.Visible && (!_compact || _branchDetailsExpanded);
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
        if (_cloudSafetyLabel != null)
        {
            _cloudSafetyLabel.Text = _compact
                ? CompactCloudSafetyDetailText()
                : $"Steam Cloud actions apply to selected version: {SteamGameBranch.DisplayName(_gameBranch)}.\nPull copies Steam Cloud saves to Android. Push copies Android saves to Steam Cloud and can overwrite remote saves.";
            _cloudSafetyLabel.Visible = !_compact || _cloudSafetyExpanded;
        }
        if (_readyVersionSummaryLabel != null)
        {
            _readyVersionSummaryLabel.Text = _compact
                ? CompactReadyVersionSummary()
                : $"Ready version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n"
                    + $"Slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}. Start Game and Pull/Push use this version.\n"
                    + "Cloud: Pull first. Push stays locked until explicitly opened.";
        }
        if (_cloudSafetyToggle != null)
        {
            _cloudSafetyToggle.Visible = _compact;
            SetCompactActionButtonText(_cloudSafetyToggle, _cloudSafetyExpanded
                ? CompactPlaySyncDrawerText("Hide Save Check", "Keep saves safe")
                : CompactCloudSafetySummary());
        }
    }
}
