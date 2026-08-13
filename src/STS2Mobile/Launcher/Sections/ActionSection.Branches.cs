using System.Collections.Generic;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void SetGameBranch(string branch)
    {
        var selection = LauncherBranchDropdown.NormalizeSelection(_gameBranch, branch);
        _gameBranch = selection.Branch;
        _versionCheckOutcome = LauncherVersionCheckOutcome.None;
        SetVersionPrimaryAction(LauncherVersionPrimaryAction.CheckForUpdates);
        UpdateHomeStateLine();
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    internal void SetAvailableBranches(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _versionCheckOutcome = LauncherVersionCheckOutcome.None;
        _availableBranches = LauncherBranchDropdown.NormalizeAvailableBranches(branches);
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    internal void SetGameBranchOptions(
        string branch,
        IReadOnlyList<LauncherBranchCatalog.BranchOption> branches
    )
    {
        _gameBranch = LauncherBranchDropdown.NormalizeSelection(_gameBranch, branch).Branch;
        _versionCheckOutcome = LauncherVersionCheckOutcome.None;
        SetVersionPrimaryAction(LauncherVersionPrimaryAction.CheckForUpdates);
        UpdateHomeStateLine();
        _availableBranches = LauncherBranchDropdown.NormalizeAvailableBranches(branches);
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    private void ApplyGameBranch(long index)
    {
        if (!LauncherBranchDropdown.TryGetBranch(_branchOptions, index, out var branch))
            return;

        SetGameBranch(branch);
        GameBranchChanged?.Invoke(_gameBranch);
    }

    private void PopulateBranchDropdown()
        => LauncherBranchDropdown.Populate(_branchDropdown, _branchOptions, _gameBranch, _availableBranches);
}
