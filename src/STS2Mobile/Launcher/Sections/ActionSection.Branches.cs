using System.Collections.Generic;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
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

    private void ToggleBranchDetails()
    {
        _branchDetailsExpanded = !_branchDetailsExpanded;
        ApplyBranchControlVisibility();
        UpdateBranchHelpText();
    }

    private void ApplyBranchControlVisibility()
    {
        if (_compact && !_branchControlsAvailable)
        {
            _branchDetailsExpanded = false;
        }

        if (_compact)
        {
            _branchDropdown.Visible = _branchControlsAvailable && _branchDetailsExpanded;
            _branchHelpLabel.Visible = _branchControlsAvailable && _branchDetailsExpanded;
            _branchDetailsToggle.Visible = _branchControlsAvailable;
            return;
        }

        _branchDropdown.Visible = _branchControlsAvailable;
        _branchHelpLabel.Visible = _branchControlsAvailable;
        _branchDetailsToggle.Visible = false;
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
        ApplyBranchControlVisibility();
        UpdateBranchHelpText();
    }

    private void PopulateBranchDropdown()
        => LauncherBranchDropdown.Populate(_branchDropdown, _branchOptions, _gameBranch, _availableBranches);
}
