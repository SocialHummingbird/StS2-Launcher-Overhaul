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

        var title = new StyledLabel(
            "Diagnostics Console",
            scale,
            fontSize: LauncherViewLayoutMetrics.LogTitleFontSize,
            align: HorizontalAlignment.Left
        );
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        drawer.AddChild(title);

        var help = new StyledLabel(
            "Hidden by default. Export sanitized diagnostics when reporting launcher issues.",
            scale,
            fontSize: 11,
            align: HorizontalAlignment.Left
        );
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        help.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        drawer.AddChild(help);

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
