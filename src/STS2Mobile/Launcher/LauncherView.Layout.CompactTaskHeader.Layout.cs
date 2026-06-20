using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static void ApplyCompactStickyTaskHeaderLayout(
        GridContainer header,
        Button compactCurrentTaskButton,
        Control workflowStrip,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var stacked = ShouldStackCompactStickyTaskHeader(profile);
        header.Columns = stacked ? 1 : 2;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                stacked ? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap,
                scale
            )
        );

        if (stacked)
        {
            compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            compactCurrentTaskButton.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactStackedCurrentTaskHeight, scale)
            );
            workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            workflowStrip.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            return;
        }

        compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        compactCurrentTaskButton.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskButtonMinWidth, scale),
            LauncherViewLayoutMetrics.ScaleInt(CompactInlineCurrentTaskHeight, scale)
        );
        workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workflowStrip.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    private static bool ShouldStackCompactStickyTaskHeader(LauncherLayoutProfile profile)
        => profile.ContentMaxWidth < LauncherViewLayoutMetrics.ScaleInt(
            CompactStickyTaskHeaderStackWidth,
            profile.Scale
        );
}
