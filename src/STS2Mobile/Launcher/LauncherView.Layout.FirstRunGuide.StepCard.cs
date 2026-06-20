using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Control BuildCompactSafeFlowStep(
        float scale,
        CompactSafeFlowStepSpec step
    )
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepHeight, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildCompactSafeFlowStepStyle(scale, step.Accent)
        );

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );

        row.AddChild(BuildCompactSafeFlowStepAccent(scale, step.Accent));
        row.AddChild(BuildCompactSafeFlowStepMarker(scale, step));
        row.AddChild(BuildCompactSafeFlowStepText(scale, step));
        panel.AddChild(row);
        return panel;
    }

    private static VBoxContainer BuildCompactSafeFlowStepText(
        float scale,
        CompactSafeFlowStepSpec step
    )
    {
        var textColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        textColumn.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);
        textColumn.AddChild(BuildCompactSafeFlowStepTitle(scale, step.Title));
        textColumn.AddChild(BuildCompactSafeFlowStepDetail(scale, step.Detail));
        return textColumn;
    }

}
