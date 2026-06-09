using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static RichTextLabel BuildLogColumn(
        LauncherLayoutProfile profile,
        VBoxContainer root,
        Action<InputEvent> dismissKeyboard
    )
    {
        var scale = profile.Scale;
        var drawer = new VBoxContainer();
        drawer.Visible = false;
        drawer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        drawer.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );

        var toggle = new StyledButton(
            "SHOW DIAGNOSTICS CONSOLE",
            scale,
            fontSize: 12,
            height: 34
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        toggle.Pressed += () =>
        {
            drawer.Visible = !drawer.Visible;
            toggle.Text = drawer.Visible ? "HIDE DIAGNOSTICS CONSOLE" : "SHOW DIAGNOSTICS CONSOLE";
        };
        root.AddChild(toggle);

        var log = BuildLogView(scale);
        log.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(profile.Compact ? 140 : 180, scale)
        );
        log.GuiInput += input => dismissKeyboard(input);
        drawer.AddChild(log);
        root.AddChild(drawer);
        return log;
    }
}
