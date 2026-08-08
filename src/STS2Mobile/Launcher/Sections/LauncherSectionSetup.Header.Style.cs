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
            new Color(0.035f, 0.06f, 0.08f, 0.92f),
            LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale)
        );
        style.BorderColor = new Color(0.08f, 0.36f, 0.42f, 0.65f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 4 : 9, scale);
        return style;
    }
}
