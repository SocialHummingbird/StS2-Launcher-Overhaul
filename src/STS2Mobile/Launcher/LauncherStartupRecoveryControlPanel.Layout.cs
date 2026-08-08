using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static CanvasLayer CreateLayer()
        => new()
        {
            Name = NodeName,
            Layer = CanvasLayerIndex,
        };

    private static ScrollContainer CreateScrollContainer()
    {
        var scroll = new ScrollContainer
        {
            FollowFocus = true,
        };
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return scroll;
    }

    private static MarginContainer CreateFrame(Vector2 viewportSize)
    {
        var margin = RecoveryMargin(viewportSize);
        var frame = new MarginContainer();
        frame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        frame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        frame.AddThemeConstantOverride("margin_left", margin);
        frame.AddThemeConstantOverride("margin_right", margin);
        frame.AddThemeConstantOverride("margin_top", RecoveryTopMargin(viewportSize));
        frame.AddThemeConstantOverride("margin_bottom", margin);
        return frame;
    }

    private static VBoxContainer CreateContainer(Vector2 viewportSize)
    {
        var margin = RecoveryMargin(viewportSize);
        var width = Math.Min(ContainerMaxWidth, Math.Max(320f, viewportSize.X - (margin * 2f)));
        var box = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(width, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        box.AddThemeConstantOverride(ThemeSeparation, ContainerSeparation);
        return box;
    }

    private static Vector2 ButtonMinimumSize(Vector2 viewportSize, float scale)
    {
        var width = Math.Min(ButtonMaxWidth, Math.Max(280f, viewportSize.X - (ContainerMargin * 2f)));
        return new Vector2(width, LauncherComponentTheme.ScaleInt(scale, (int)ButtonHeight));
    }

    private static bool UseCompactRecoveryCopy(Vector2 viewportSize)
        => OperatingSystem.IsAndroid() || Math.Min(viewportSize.X, viewportSize.Y) < 720f;

    private static int RecoveryMargin(Vector2 viewportSize)
        => LauncherComponentTheme.ScaleInt(LayoutScale(viewportSize), OperatingSystem.IsAndroid() ? 16 : (int)ContainerMargin);

    private static int RecoveryTopMargin(Vector2 viewportSize)
        => LauncherComponentTheme.ScaleInt(
            LayoutScale(viewportSize),
            OperatingSystem.IsAndroid() ? 18 : (int)Math.Min(ContainerTop, Math.Max(16f, viewportSize.Y * 0.06f))
        );
}
