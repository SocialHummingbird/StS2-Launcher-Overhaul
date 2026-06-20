using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private Button BuildBranchDetailsToggle(float scale, bool compact)
    {
        var button = new StyledButton(
            compact ? "" : "Show Version Details",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(button, scale);
        button.Visible = compact;
        button.Pressed += ToggleBranchDetails;
        return button;
    }
}
