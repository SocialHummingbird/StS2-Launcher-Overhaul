using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private void UpdateBranchHelpText()
    {
        _branchHelpLabel.Text = _compact
            ? CompactInstallVersionHelpText()
            : SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
                + "\n"
                + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches)
                + "\n"
                + "Download/update changes local files for the selected game version only; it does not change Steam Cloud saves.";
        ApplyBranchControlVisibility();
        if (_branchDetailsToggle != null)
        {
            if (_compact)
            {
                SetCompactVersionActionButtonText(
                    _branchDetailsToggle,
                    _branchDetailsExpanded ? "Hide Version" : "Change Version",
                    _branchDetailsExpanded ? "Keep selection" : "Local files only"
                );
            }
            else
            {
                _branchDetailsToggle.Text = _branchDetailsExpanded
                    ? "Hide Version Details"
                    : "Show Version Details";
            }
        }
        if (_compactSelectedVersionLabel != null)
        {
            _compactSelectedVersionLabel.Text = _compact
                ? CompactSelectedVersionHeadline()
                : $"Selected version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n"
                    + $"Install slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}. Downloads do not change Steam Cloud saves.";
            _compactSelectedVersionLabel.Visible = _compact;
        }
        if (_compactSelectedVersionPanel != null)
            _compactSelectedVersionPanel.Visible = _compact;
    }
}
