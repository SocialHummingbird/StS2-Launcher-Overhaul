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
}
