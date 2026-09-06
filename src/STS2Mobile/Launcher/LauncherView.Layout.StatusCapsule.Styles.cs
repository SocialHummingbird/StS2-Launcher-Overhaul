using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static void ApplyCompactStatusDetailButtonStyle(Button button, float scale)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildCompactStatusDetailButtonStyle(
                scale,
                LauncherComponentTheme.ButtonNormal,
                new Color(0.05f, 0.34f, 0.42f, 0.4f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildCompactStatusDetailButtonStyle(
                scale,
                LauncherComponentTheme.ButtonNormal,
                new Color(0.06f, 0.54f, 0.62f, 0.58f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildCompactStatusDetailButtonStyle(
                scale,
                LauncherComponentTheme.ButtonNormal,
                new Color(0.95f, 0.42f, 0.08f, 0.68f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildCompactStatusDetailButtonStyle(
                scale,
                LauncherComponentTheme.ButtonNormal,
                new Color(0.05f, 0.16f, 0.2f, 0.24f)
            )
        );
    }

    private static StyleBoxFlat BuildCompactStatusDetailButtonStyle(float scale, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRadius, scale)
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            LauncherComponentTheme.ButtonNormal,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = LauncherComponentTheme.ButtonHover;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 8, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusPhaseStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            LauncherComponentTheme.ButtonNormal,
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );
        style.BorderColor = LauncherComponentTheme.ButtonHover;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        return style;
    }
}
