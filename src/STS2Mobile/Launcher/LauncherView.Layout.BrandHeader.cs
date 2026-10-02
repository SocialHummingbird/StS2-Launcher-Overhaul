using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherView
{
    private const int CompactBrandTitleFontSize = 18;
    private const int CompactBrandRowSeparation = 6;
    private const int CompactBrandHeaderSeparation = 2;
    private static Control BuildBrandHeader(LauncherLayoutProfile profile)
    {
        if (profile.Compact)
            return BuildCompactBrandHeader(profile);
        var scale = profile.Scale;
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(6, scale));
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(10, scale));
        row.AddChild(BuildBrandMark(scale, compact: false));
        row.AddChild(BuildDesktopBrandCopy(scale));
        header.AddChild(row);
        header.AddChild(BuildBrandDivider(scale, height: 2));
        return header;
    }

    private static ColorRect BuildBrandDivider(float scale, int height) => new()
    {
        Color = LauncherComponentTheme.CyanDim,
        CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(height, scale)),
    };
    private static Control BuildCompactBrandHeader(LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactBrandHeaderSeparation, scale));
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactBrandRowSeparation, scale));
        row.AddChild(BuildBrandMark(scale, compact: true));
        row.AddChild(BuildCompactBrandTitle(scale));
        header.AddChild(row);
        header.AddChild(BuildBrandDivider(scale, height: 1));
        return header;
    }

    private static StyledLabel BuildCompactBrandTitle(float scale)
    {
        var title = new StyledLabel("StS2 Launcher", scale, fontSize: CompactBrandTitleFontSize);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.ClipText = true;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        title.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextPrimary);
        return title;
    }

    private static Control BuildDesktopBrandCopy(float scale)
    {
        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(0, scale));
        var title = new StyledLabel("StS2 Launcher", scale, fontSize: 26);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextPrimary);
        copy.AddChild(title);
        return copy;
    }

    private const int CompactBrandMarkHeight = 26;
    private static Control BuildBrandMark(float scale, bool compact)
    {
        var height = compact ? CompactBrandMarkHeight : 50;
        var mark = new HBoxContainer();
        mark.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(compact ? 12 : 16, scale), LauncherViewLayoutMetrics.ScaleInt(height, scale));
        mark.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(compact ? 2 : 3, scale));
        mark.AddChild(BuildBrandMarkStripe(LauncherComponentTheme.OrangeAccent, compact ? 5 : 6, height, scale));
        mark.AddChild(BuildBrandMarkStripe(LauncherComponentTheme.CyanAccent, compact ? 2 : 3, height, scale));
        return mark;
    }

    private static ColorRect BuildBrandMarkStripe(Color color, int width, int height, float scale) => new()
    {
        Color = color,
        CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(width, scale), LauncherViewLayoutMetrics.ScaleInt(height, scale)),
    };
}
