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
        row.AddChild(BuildCompactBrandSubtitle(scale));
        header.AddChild(row);
        header.AddChild(BuildBrandDivider(scale, height: 1));
        return header;
    }

    private static StyledLabel BuildCompactBrandTitle(float scale)
    {
        var title = new StyledLabel("StS2 Mobile", scale, fontSize: CompactBrandTitleFontSize);
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

    private static StyledLabel BuildCompactBrandSubtitle(float scale)
    {
        var subtitle = new StyledLabel(
            "Saves safe. Ready to play.",
            scale,
            fontSize: CompactBrandSubtitleFontSize,
            align: HorizontalAlignment.Right
        );
        subtitle.VerticalAlignment = VerticalAlignment.Center;
        subtitle.ClipText = true;
        subtitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        subtitle.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        return subtitle;
    }
}
