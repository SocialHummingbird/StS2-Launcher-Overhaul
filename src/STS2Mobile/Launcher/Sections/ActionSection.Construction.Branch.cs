using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (VBoxContainer Group, OptionButton Dropdown, Label StateLabel) BuildBranchControls(
        float scale,
        bool compact
    )
    {
        var group = new VBoxContainer
        {
            Name = "GameVersionSelection",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );

        var title = new StyledLabel(
            "Game version",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        title.Name = "GameVersionLabel";
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        group.AddChild(title);

        var branchDropdown = new OptionButton
        {
            FitToLongestItem = !compact,
        };
        branchDropdown.AccessibilityName = "Game version";
        branchDropdown.AccessibilityDescription = "Select the game version Play will use.";
        branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        branchDropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(
                LauncherSectionMetrics.SecondaryButtonHeight,
                scale
            )
        );
        LauncherButtonStyles.ApplyDropdownAction(
            branchDropdown,
            scale,
            compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
        );
        branchDropdown.ItemSelected += ApplyGameBranch;
        group.AddChild(branchDropdown);

        var branchHelpLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailLabelFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        branchHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        branchHelpLabel.Name = "SelectedGameVersionState";
        branchHelpLabel.VerticalAlignment = VerticalAlignment.Top;
        branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        branchHelpLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        group.AddChild(branchHelpLabel);
        AddChild(group);

        return (group, branchDropdown, branchHelpLabel);
    }
}
