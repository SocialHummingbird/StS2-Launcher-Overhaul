using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private static StyleBoxFlat BuildAndroidPanelStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(
                LauncherComponentTheme.PanelBackground.R,
                LauncherComponentTheme.PanelBackground.G,
                LauncherComponentTheme.PanelBackground.B,
                0.92f
            ),
            LauncherComponentTheme.ScaleInt(scale, AndroidPanelRadius)
        );
        style.BorderColor = LauncherComponentTheme.CyanDim;
        style.SetBorderWidthAll(Math.Max(1, LauncherComponentTheme.ScaleInt(scale, 1)));
        style.ContentMarginLeft = LauncherComponentTheme.ScaleInt(scale, AndroidPanelHorizontalMargin);
        style.ContentMarginRight = LauncherComponentTheme.ScaleInt(scale, AndroidPanelHorizontalMargin);
        style.ContentMarginTop = LauncherComponentTheme.ScaleInt(scale, AndroidPanelVerticalMargin);
        style.ContentMarginBottom = LauncherComponentTheme.ScaleInt(scale, AndroidPanelVerticalMargin);
        return style;
    }
}
