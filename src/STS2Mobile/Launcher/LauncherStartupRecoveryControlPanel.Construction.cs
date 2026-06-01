using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private LauncherStartupRecoveryControlPanel(string nodeName)
    {
        Layer = new CanvasLayer
        {
            Name = nodeName,
            Layer = CanvasLayerIndex,
        };

        var box = new VBoxContainer
        {
            Position = ContainerPosition,
            CustomMinimumSize = ContainerMinimumSize,
        };
        box.AddThemeConstantOverride(ThemeSeparation, ContainerSeparation);
        Layer.AddChild(box);

        var title = new Label
        {
            Text = "Game is starting.",
        };
        title.AddThemeFontSizeOverride(ThemeFontSize, TitleFontSize);
        title.AddThemeColorOverride(ThemeFontColor, TitleColor);
        box.AddChild(title);

        _detail = new Label
        {
            Text = "If this screen does not change, copy the raw error log, export diagnostics, or restart with safe launch. These controls stay visible for several minutes.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _detail.AddThemeFontSizeOverride(ThemeFontSize, DetailFontSize);
        _detail.AddThemeColorOverride(ThemeFontColor, DetailColor);
        box.AddChild(_detail);

        AddButton(box, "RETURN TO LAUNCHER", AndroidGodotAppBridge.RestartApp);
        AddButton(
            box,
            "RESTART WITH SAFE LAUNCH",
            () =>
            {
                LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
                AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
            }
        );
        AddButton(box, "EXPORT STARTUP DIAGNOSTICS", ExportDiagnostics);
        AddButton(box, "COPY RAW ERROR LOG", CopyRawErrorLog);
        AddButton(box, "HIDE RECOVERY CONTROLS", () => Layer.QueueFree());
    }

    private static void AddButton(VBoxContainer box, string label, Action run)
    {
        var button = new Button
        {
            Text = label,
            CustomMinimumSize = ButtonMinimumSize,
        };
        button.Pressed += run;
        box.AddChild(button);
    }
}
