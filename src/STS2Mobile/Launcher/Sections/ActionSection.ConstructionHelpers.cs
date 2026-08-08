using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static VBoxContainer BuildActionGroup(float scale)
    {
        var group = new VBoxContainer();
        group.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale)
        );
        return group;
    }

    private static Container BuildCompactCloudPrimaryActionsRow(
        Container parent,
        float scale,
        bool compactStackedActionRows
    )
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCloudPrimaryActionSeparation, scale)
        );
        parent.AddChild(row);
        return row;
    }

    private static Container BuildCompactCloudOptionsRow(
        Container parent,
        float scale,
        bool compactStackedActionRows
    )
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.Visible = false;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCloudOptionToggleSeparation, scale)
        );
        parent.AddChild(row);
        return row;
    }
}
