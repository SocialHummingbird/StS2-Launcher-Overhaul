using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Control BuildCompactBrandHeader(LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactBrandHeaderSeparation, scale)
        );

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactBrandRowSeparation, scale)
        );
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
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        return title;
    }
}
