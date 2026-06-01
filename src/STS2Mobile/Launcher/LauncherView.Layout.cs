using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (StyledPanel Panel, HBoxContainer RootColumns) BuildShell(
        Control parent,
        float scale,
        Action<InputEvent> dismissKeyboard
    )
    {
        parent.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ScreenBackground();
        background.GuiInput += input => dismissKeyboard(input);
        parent.AddChild(background);

        var panel = new StyledPanel(scale, widthRatio: 0.9f);
        panel.UpdateSizeFromViewport(
            parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080)
        );
        panel.Panel.GuiInput += input => dismissKeyboard(input);
        parent.AddChild(panel);

        var rootColumns = new HBoxContainer();
        rootColumns.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rootColumns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        rootColumns.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.RootColumnSeparation, scale)
        );
        panel.Content.AddChild(rootColumns);

        return (Panel: panel, RootColumns: rootColumns);
    }
}
