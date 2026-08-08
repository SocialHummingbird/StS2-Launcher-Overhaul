using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactBrandTitleFontSize = 18;
    private const int CompactBrandSubtitleFontSize = 12;
    private const int CompactBrandRowSeparation = 6;
    private const int CompactBrandHeaderSeparation = 2;

    private static Control BuildBrandHeader(LauncherLayoutProfile profile)
    {
        if (profile.Compact)
            return BuildCompactBrandHeader(profile);

        var scale = profile.Scale;
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(10, scale)
        );

        row.AddChild(BuildBrandMark(scale, compact: false));
        row.AddChild(BuildDesktopBrandCopy(scale));
        header.AddChild(row);
        header.AddChild(BuildBrandDivider(scale, height: 2));
        return header;
    }

    private static ColorRect BuildBrandDivider(float scale, int height)
        => new()
        {
            Color = LauncherComponentTheme.CyanDim,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(height, scale)
            ),
        };
}
