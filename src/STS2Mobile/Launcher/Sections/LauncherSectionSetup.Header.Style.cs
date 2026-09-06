using System;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal static partial class LauncherSectionSetup
{
    private static StyleBoxFlat BuildHeaderStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            LauncherComponentTheme.ButtonNormal,
            LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale)
        );
        style.BorderColor = LauncherComponentTheme.ButtonHover;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 9, scale);
        return style;
    }
}
