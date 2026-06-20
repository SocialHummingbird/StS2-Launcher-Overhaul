using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static VBoxContainer BuildCompactWorkflowStepBody(float scale)
    {
        var body = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepSeparation, scale)
        );
        return body;
    }

    private static HBoxContainer BuildCompactWorkflowLabelRow(float scale)
    {
        var labelRow = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        labelRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberGap, scale)
        );
        return labelRow;
    }

    private static ColorRect BuildCompactWorkflowAccent(float scale)
    {
        return new ColorRect
        {
            Color = LauncherComponentTheme.ButtonNormal,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepAccentHeight, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }
}
