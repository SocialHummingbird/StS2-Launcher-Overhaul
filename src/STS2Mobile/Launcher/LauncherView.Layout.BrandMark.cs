using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactBrandMarkHeight = 26;

    private static Control BuildBrandMark(float scale, bool compact)
    {
        var height = compact ? CompactBrandMarkHeight : 50;
        var mark = new HBoxContainer();
        mark.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(compact ? 12 : 16, scale),
            LauncherViewLayoutMetrics.ScaleInt(height, scale)
        );
        mark.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(compact ? 2 : 3, scale)
        );

        mark.AddChild(BuildBrandMarkStripe(
            LauncherComponentTheme.OrangeAccent,
            compact ? 5 : 6,
            height,
            scale
        ));
        mark.AddChild(BuildBrandMarkStripe(
            LauncherComponentTheme.CyanAccent,
            compact ? 2 : 3,
            height,
            scale
        ));
        return mark;
    }

    private static ColorRect BuildBrandMarkStripe(
        Color color,
        int width,
        int height,
        float scale
    )
        => new()
        {
            Color = color,
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(width, scale),
                LauncherViewLayoutMetrics.ScaleInt(height, scale)
            ),
        };
}
