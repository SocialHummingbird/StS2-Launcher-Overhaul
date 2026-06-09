using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (StyledPanel Panel, VBoxContainer Content) BuildShell(
        Control parent,
        LauncherLayoutProfile profile,
        Action<InputEvent> dismissKeyboard
    )
    {
        parent.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ScreenBackground();
        background.GuiInput += input => dismissKeyboard(input);
        parent.AddChild(background);

        var panel = new StyledPanel(profile.Scale, widthRatio: profile.PanelWidthRatio);
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
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.RootColumnSeparation, profile.Scale)
        );
        panel.AddContent(content);

        content.AddChild(BuildBrandHeader(profile));
        return (panel, content);
    }

    private static Control BuildBrandHeader(LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(4, scale)
        );

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(10, scale)
        );

        var mark = new ColorRect();
        mark.Color = LauncherComponentTheme.OrangeAccent;
        mark.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(6, scale),
            LauncherViewLayoutMetrics.ScaleInt(48, scale)
        );
        row.AddChild(mark);

        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(0, scale)
        );

        var title = new StyledLabel("StS2 Launcher", scale, fontSize: profile.Compact ? 22 : 26);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        copy.AddChild(title);

        var subtitle = new StyledLabel("ANDROID CLOUD LAUNCH TERMINAL", scale, fontSize: 11);
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
        line.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(2, scale));
        header.AddChild(line);
        return header;
    }
}
