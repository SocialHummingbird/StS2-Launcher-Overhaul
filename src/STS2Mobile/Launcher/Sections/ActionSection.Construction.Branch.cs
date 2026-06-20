using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (Button DetailsToggle, OptionButton Dropdown, Label HelpLabel) BuildBranchControls(
        float scale,
        bool compact
    )
    {
        var branchDetailsToggle = new StyledButton(
            "Show Version Details",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(branchDetailsToggle, scale);
        branchDetailsToggle.Visible = false;
        branchDetailsToggle.Pressed += ToggleBranchDetails;
        AddChild(branchDetailsToggle);

        var branchDropdown = new OptionButton();
        branchDropdown.Visible = false;
        branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        branchDropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.PrimaryButtonHeight : LauncherSectionMetrics.SecondaryButtonHeight,
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
        AddChild(branchDropdown);

        var branchHelpLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactReadyVersionHelpFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        branchHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        branchHelpLabel.ClipText = compact;
        branchHelpLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            branchHelpLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            branchHelpLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactReadyVersionHelpHeight, scale)
            );
        }
        branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        branchHelpLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        branchHelpLabel.Visible = false;
        AddChild(branchHelpLabel);

        return (branchDetailsToggle, branchDropdown, branchHelpLabel);
    }
}
