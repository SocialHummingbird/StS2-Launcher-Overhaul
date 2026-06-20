using System.Collections.Generic;
using STS2Mobile.Launcher;

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

    private void PopulateBranchDropdown()
        => LauncherBranchDropdown.Populate(_branchDropdown, _branchOptions, _gameBranch, _availableBranches);

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
}
