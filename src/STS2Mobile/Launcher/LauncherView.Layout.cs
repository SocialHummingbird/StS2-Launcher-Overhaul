using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactBrandTitleFontSize = 18;
    private const int CompactBrandSubtitleFontSize = 12;
    private const int CompactBrandMarkHeight = 26;
    private const int CompactBrandRowSeparation = 6;
    private const int CompactBrandHeaderSeparation = 2;

    private static (StyledPanel Panel, VBoxContainer Content) BuildShell(
        Control parent,
        LauncherLayoutProfile profile,
        Action<InputEvent> dismissKeyboard
    )
    {
        parent.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = LauncherComponentTheme.ScreenBackground,
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        background.GuiInput += input => dismissKeyboard(input);
        parent.AddChild(background);

        var panel = new StyledPanel(profile.Scale, widthRatio: profile.PanelWidthRatio, compact: profile.Compact);
        panel.UpdateSizeFromViewport(
            parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080),
            profile.PanelHeightRatio
        );
        panel.OnPanelGuiInput(dismissKeyboard);
        parent.AddChild(panel);

        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                profile.Compact
                    ? LauncherViewLayoutMetrics.CompactRootColumnSeparation
                    : LauncherViewLayoutMetrics.RootColumnSeparation,
                profile.Scale
            )
        );
        panel.AddContent(content);

        content.AddChild(BuildBrandHeader(profile));
        return (panel, content);
    }

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

        var mark = BuildBrandMark(scale, compact: false);
        row.AddChild(mark);

        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(0, scale)
        );

        var title = new StyledLabel("StS2 Mobile", scale, fontSize: 26);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        copy.AddChild(title);

        var subtitle = new StyledLabel(
            "STEAM LOGIN  |  VERSION SLOTS  |  CLOUD SAVES",
            scale,
            fontSize: 11
        );
        subtitle.HorizontalAlignment = HorizontalAlignment.Left;
        subtitle.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        copy.AddChild(subtitle);
        row.AddChild(copy);
        header.AddChild(row);

        var line = new ColorRect();
        line.Color = LauncherComponentTheme.CyanDim;
        line.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(2, scale)
        );
        header.AddChild(line);
        return header;
    }

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
        row.AddChild(title);

        var subtitle = new StyledLabel(
            "STEAM | CLOUD | PLAY",
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
        row.AddChild(subtitle);
        header.AddChild(row);

        var line = new ColorRect();
        line.Color = LauncherComponentTheme.CyanDim;
        line.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(1, scale)
        );
        header.AddChild(line);
        return header;
    }

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

        var hot = new ColorRect();
        hot.Color = LauncherComponentTheme.OrangeAccent;
        hot.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(compact ? 5 : 6, scale),
            LauncherViewLayoutMetrics.ScaleInt(height, scale)
        );
        mark.AddChild(hot);

        var cold = new ColorRect();
        cold.Color = LauncherComponentTheme.CyanAccent;
        cold.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(compact ? 2 : 3, scale),
            LauncherViewLayoutMetrics.ScaleInt(height, scale)
        );
        mark.AddChild(cold);

        return mark;
    }
}
