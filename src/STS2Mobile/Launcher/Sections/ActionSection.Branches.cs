using System.Collections.Generic;
using STS2Mobile.Steam;
using Godot;
using STS2Mobile.Launcher.Components;

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

    internal void SetGameBranchOptions(string branch, IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
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

    private void PopulateBranchDropdown() => LauncherBranchDropdown.Populate(_branchDropdown, _branchOptions, _gameBranch, _availableBranches);
    private void UpdateBranchHelpText()
    {
        _selectedVersionInstalled = LauncherBranchCatalog.SelectedVersionInstalled(_gameBranch, _availableBranches);
        var state = LauncherBranchCatalog.SelectedVersionState(_gameBranch, _availableBranches);
        _branchHelpLabel.Text = _versionCheckOutcome switch
        {
            LauncherVersionCheckOutcome.UpToDate => state + " · Up to date",
            LauncherVersionCheckOutcome.UpdateAvailable => state + " · Update available",
            _ => state,
        };
        _branchHelpLabel.TooltipText = LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches);
        _branchHelpLabel.AccessibilityDescription = _branchHelpLabel.TooltipText;
        UpdateVersionActionAvailability();
    }

    private (VBoxContainer Group, OptionButton Dropdown, Label StateLabel) BuildBranchControls(float scale, bool compact)
    {
        var group = new VBoxContainer
        {
            Name = "GameVersionSelection",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(6, scale));
        var title = new StyledLabel("Game version", scale, fontSize: compact ? LauncherSectionMetrics.CompactVersionSummaryFontSize : LauncherSectionMetrics.ProgressFontSize, align: HorizontalAlignment.Left);
        title.Name = "GameVersionLabel";
        title.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextPrimary);
        group.AddChild(title);
        var branchDropdown = new OptionButton
        {
            FitToLongestItem = !compact,
        };
        branchDropdown.AccessibilityName = "Game version";
        branchDropdown.AccessibilityDescription = "Select the game version Play will use.";
        branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        branchDropdown.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SecondaryButtonHeight, scale));
        LauncherButtonStyles.ApplyDropdownAction(branchDropdown, scale, compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize, compact);
        branchDropdown.ItemSelected += ApplyGameBranch;
        group.AddChild(branchDropdown);
        var branchHelpLabel = new StyledLabel("", scale, fontSize: compact ? LauncherSectionMetrics.CompactDetailLabelFontSize : LauncherSectionMetrics.ProgressFontSize, align: HorizontalAlignment.Left);
        branchHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        branchHelpLabel.Name = "SelectedGameVersionState";
        branchHelpLabel.VerticalAlignment = VerticalAlignment.Top;
        branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        branchHelpLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherViewLayoutMetrics.LogTitleColor);
        group.AddChild(branchHelpLabel);
        AddChild(group);
        return (group, branchDropdown, branchHelpLabel);
    }
}
