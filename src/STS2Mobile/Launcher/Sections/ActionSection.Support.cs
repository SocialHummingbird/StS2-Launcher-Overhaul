using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static GridContainer BuildCompactSupportToolsGrid(
        float scale,
        bool compact,
        bool compactStackedActionRows
    )
    {
        var grid = new GridContainer
        {
            Columns = compactStackedActionRows ? 1 : 2,
            Visible = compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        return grid;
    }

    private void ToggleSupportOptions()
    {
        _supportExpanded = !_supportExpanded;
        _supportGroup.Visible = _supportExpanded;
        SetCompactActionButtonText(_supportToggle, _supportExpanded
            ? (_compact ? CompactPlaySyncDrawerText("Hide Fixes", "Back to play") : "Hide Support Options")
            : SupportToggleText());
    }

    private string SupportToggleText()
        => _compact ? CompactPlaySyncDrawerText("Fixes & Help", "Repair tools") : "More Support Options";
}
