using System.Collections.Generic;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class DownloadSection
{
    internal void SetGameBranch(string branch)
    {
        var selection = LauncherBranchDropdown.NormalizeSelection(_gameBranch, branch);
        _gameBranch = selection.Branch;
        PopulateBranchDropdown();
        if (selection.Changed)
        {
            CollapseCompactBranchDetailsAfterSelection();
            return;
        }

        UpdateBranchHelpText();
    }

    internal void SetAvailableBranches(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _availableBranches = LauncherBranchDropdown.NormalizeAvailableBranches(branches);
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    internal void SetGameBranchOptions(string branch, IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _gameBranch = LauncherBranchDropdown.NormalizeSelection(_gameBranch, branch).Branch;
        _availableBranches = LauncherBranchDropdown.NormalizeAvailableBranches(branches);
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    private void ApplyBranchControlVisibility()
    {
        var controlsVisible = !_compact || _branchDetailsExpanded;
        if (_compactVersionControlsRow != null)
            _compactVersionControlsRow.Visible = controlsVisible;
        _branchDropdown.Visible = controlsVisible;
        _refreshBranchesButton.Visible = controlsVisible;
        _branchHelpLabel.Visible = controlsVisible;
    }

    private void ApplyGameBranch(long index)
    {
        if (!LauncherBranchDropdown.TryGetBranch(_branchOptions, index, out var branch))
            return;
        SetGameBranch(branch);
        CollapseCompactBranchDetailsAfterSelection();
        GameBranchChanged?.Invoke(_gameBranch);
    }

    private void CollapseCompactBranchDetailsAfterSelection()
    {
        if (!_compact)
            return;
        _branchDetailsExpanded = false;
        UpdateBranchHelpText();
    }

    private void PopulateBranchDropdown() => LauncherBranchDropdown.Populate(_branchDropdown, _branchOptions, _gameBranch, _availableBranches);
    private void ToggleBranchDetails()
    {
        _branchDetailsExpanded = !_branchDetailsExpanded;
        UpdateBranchHelpText();
    }

    private void OpenCompactBranchDetailsFromSelectedVersion()
    {
        if (!_compact)
            return;
        _branchDetailsExpanded = true;
        UpdateBranchHelpText();
    }

    private void UpdateBranchHelpText()
    {
        _branchHelpLabel.Text = _compact ? CompactInstallVersionHelpText() : SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch) + "\n" + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches) + "\n" + "Updates replace the selected Steam branch only after the new files pass checks. Recovery removes only that branch's downloaded files. Saves and other branches stay in place.";
        ApplyBranchControlVisibility();
        if (_branchDetailsToggle != null)
        {
            if (_compact)
            {
                SetCompactVersionActionButtonText(_branchDetailsToggle, _branchDetailsExpanded ? "Hide Version" : "Change Version", _branchDetailsExpanded ? "Keep selection" : "Local files only");
            }
            else
            {
                _branchDetailsToggle.Text = _branchDetailsExpanded ? "Hide Version Details" : "Show Version Details";
            }
        }

        if (_compactSelectedVersionLabel != null)
        {
            _compactSelectedVersionLabel.Text = _compact ? CompactSelectedVersionHeadline() : $"Selected version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n" + $"Install slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}.";
            _compactSelectedVersionLabel.Visible = _compact;
        }

        if (_compactSelectedVersionPanel != null)
            _compactSelectedVersionPanel.Visible = _compact;
    }
}
