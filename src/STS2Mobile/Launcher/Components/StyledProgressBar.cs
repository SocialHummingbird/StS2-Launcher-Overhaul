using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledProgressBar : ProgressBar
{
    private const string BackgroundStyle = "background";
    private const string FillStyle = "fill";
    private const string FontColor = "font_color";

    internal StyledProgressBar(float scale, bool compact = false)
    {
        CustomMinimumSize = new Vector2(
            0,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? LauncherComponentTheme.CompactProgressBarHeight
                    : LauncherComponentTheme.ProgressBarHeight
            )
        );
        ShowPercentage = true;
        AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? LauncherComponentTheme.CompactProgressBarFontSize
                    : LauncherComponentTheme.ProgressBarFontSize
            )
        );
        AddThemeColorOverride(FontColor, LauncherComponentTheme.TextPrimary);
        AddThemeStyleboxOverride(
            BackgroundStyle,
            BuildProgressStyle(scale, LauncherComponentTheme.ProgressBackground, compact)
        );
        AddThemeStyleboxOverride(
            FillStyle,
            BuildProgressStyle(
                scale,
                compact
                    ? LauncherComponentTheme.ProgressFillCompact
                    : LauncherComponentTheme.ProgressFill,
                compact
            )
        );
    }

    private static StyleBoxFlat BuildProgressStyle(float scale, Color color, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            color,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? LauncherComponentTheme.ButtonRadius
                    : LauncherComponentTheme.ProgressBarRadius
            )
        );
        style.BorderColor = LauncherComponentTheme.CyanDim;
        style.SetBorderWidthAll(Math.Max(1, LauncherComponentTheme.ScaleInt(scale, compact ? 1 : 0)));
        return style;
    }
}
