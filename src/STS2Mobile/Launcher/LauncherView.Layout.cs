using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private readonly struct LauncherShell
    {
        private LauncherShell(StyledPanel panel, HBoxContainer rootColumns)
        {
            Panel = panel;
            RootColumns = rootColumns;
        }

        private StyledPanel Panel { get; }
        private HBoxContainer RootColumns { get; }

        internal static LauncherShell Create(StyledPanel panel, HBoxContainer rootColumns)
            => new(panel, rootColumns);

        internal StyledPanel PanelControl()
            => Panel;

        internal float PanelBaseY()
            => Panel.Position.Y;

        internal PrimaryColumnControls BuildPrimaryColumn(float scale)
            => LauncherView.BuildPrimaryColumn(scale, RootColumns);

        internal RichTextLabel BuildLogColumn(
            float scale,
            Action<InputEvent> dismissKeyboard
        )
            => LauncherView.BuildLogColumn(scale, RootColumns, dismissKeyboard);
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
        panel.OnPanelGuiInput(dismissKeyboard);
        parent.AddChild(panel);

        var rootColumns = new HBoxContainer();
        rootColumns.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rootColumns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        rootColumns.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.RootColumnSeparation, scale)
        );
        panel.AddContent(rootColumns);

        return LauncherShell.Create(panel, rootColumns);
    }
}
