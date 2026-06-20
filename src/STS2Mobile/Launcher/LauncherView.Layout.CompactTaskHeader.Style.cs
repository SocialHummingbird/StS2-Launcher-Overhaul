using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Control WrapCompactStickyTaskHeader(float scale, Control header)
    {
        var toolbar = new PanelContainer();
        toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildCompactStickyTaskHeaderStyle(scale)
        );
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddChild(header);
        return toolbar;
    }

    private static StyleBoxFlat BuildCompactStickyTaskHeaderStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.018f, 0.035f, 0.045f, 0.9f),
            LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarRadius, scale)
        );
        style.BorderColor = new Color(0.04f, 0.42f, 0.5f, 0.45f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        return style;
    }
}
