using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static ColorRect BuildCompactSafeFlowStepAccent(float scale, Color accent)
        => new()
        {
            Color = accent,
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepAccentWidth, scale),
                0
            ),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

    private static StyledLabel BuildCompactSafeFlowStepMarker(
        float scale,
        CompactSafeFlowStepSpec step
    )
    {
        var markerLabel = new StyledLabel(
            step.Marker,
            scale,
            fontSize: CompactSafeFlowGuideStepNumberFontSize,
            align: HorizontalAlignment.Center
        )
        {
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepNumberWidth, scale),
                0
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
        };
        markerLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            step.Accent
        );
        return markerLabel;
    }
}
