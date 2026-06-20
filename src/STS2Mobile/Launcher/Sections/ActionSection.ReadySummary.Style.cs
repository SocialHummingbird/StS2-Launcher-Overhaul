using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static void ApplyReadyVersionSummaryButtonStyle(Button button, float scale, bool compact)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildReadyVersionSummaryStyle(scale, compact)
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.045f, 0.085f, 0.095f, 0.95f),
                new Color(0.04f, 0.72f, 0.8f, 0.78f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.05f, 0.06f, 0.98f),
                new Color(0.95f, 0.42f, 0.08f, 0.72f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.04f, 0.048f, 0.58f),
                new Color(0.05f, 0.22f, 0.26f, 0.32f)
            )
        );
    }

    private static StyleBoxFlat BuildReadyVersionSummaryStyle(float scale, bool compact)
        => BuildReadyVersionSummaryStyle(
            scale,
            compact,
            new Color(0.035f, 0.065f, 0.075f, 0.9f),
            new Color(0.04f, 0.55f, 0.62f, 0.65f)
        );

    private static StyleBoxFlat BuildReadyVersionSummaryStyle(
        float scale,
        bool compact,
        Color body,
        Color border
    )
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.CompactVersionSummaryRadius : 8,
                scale
            )
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 9,
            scale
        );
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 10,
            scale
        );
        return style;
    }
}
