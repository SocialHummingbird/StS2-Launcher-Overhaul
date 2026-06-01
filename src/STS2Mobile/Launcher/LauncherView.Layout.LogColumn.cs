using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LogView BuildLogColumn(
        float scale,
        HBoxContainer hbox,
        Action<InputEvent> dismissKeyboard
    )
    {
        var right = new VBoxContainer();
        right.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        right.SizeFlagsStretchRatio = LauncherViewLayoutMetrics.LogColumnStretchRatio;
        hbox.AddChild(right);

        var logTitle = new StyledLabel(
            "Console",
            scale,
            fontSize: LauncherViewLayoutMetrics.LogTitleFontSize
        );
        logTitle.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        right.AddChild(logTitle);

        var log = new LogView(scale);
        log.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        log.GuiInput += dismissKeyboard;
        right.AddChild(log);
        return log;
    }
}
