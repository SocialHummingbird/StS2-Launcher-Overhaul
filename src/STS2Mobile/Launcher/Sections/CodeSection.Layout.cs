using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
    private const int CompactCodeActionRowSeparation = 6;

    internal void UpdateViewportProfile(LauncherLayoutProfile profile)
    {
        if (!_compact || !GodotObject.IsInstanceValid(_compactCodeActionRow))
            return;

        _compactStackedActionRows = profile.Compact && profile.CompactStackedActionRows;
        ApplyCompactCodeActionRowLayout(_compactCodeActionRow, profile.Scale, _compactStackedActionRows);
    }

    private static GridContainer BuildCompactCodeActionRow(float scale, bool compactStackedActionRows)
    {
        var row = new GridContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ApplyCompactCodeActionRowLayout(row, scale, compactStackedActionRows);
        return row;
    }

    private static void ApplyCompactCodeActionRowLayout(
        GridContainer row,
        float scale,
        bool compactStackedActionRows
    )
    {
        row.Columns = compactStackedActionRows ? 1 : 2;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCodeActionRowSeparation, scale)
        );
    }
}
