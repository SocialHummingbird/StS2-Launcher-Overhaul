using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private readonly struct LauncherShell
    {
        internal LauncherShell(StyledPanel panel, HBoxContainer rootColumns)
        {
            Panel = panel;
            RootColumns = rootColumns;
        }

        internal StyledPanel Panel { get; }
        internal HBoxContainer RootColumns { get; }
    }

    private static LauncherShell BuildShell(
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

        return new LauncherShell(panel, rootColumns);
    }
}
