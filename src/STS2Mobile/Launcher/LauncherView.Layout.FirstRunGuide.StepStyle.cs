using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static StyleBoxFlat BuildCompactSafeFlowStepStyle(float scale, Color accent)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.03f, 0.055f, 0.07f, 0.88f),
            LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepRadius, scale)
        );
        style.BorderColor = new Color(accent.R, accent.G, accent.B, 0.3f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepHorizontalMargin,
            scale
        );
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepHorizontalMargin,
            scale
        );
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepVerticalMargin,
            scale
        );
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepVerticalMargin,
            scale
        );
        return style;
    }
}
