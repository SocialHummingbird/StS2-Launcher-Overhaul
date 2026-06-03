using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private LauncherStartupRecoveryControlPanel()
    {
        Layer = CreateLayer();

        var box = CreateContainer();
        Layer.AddChild(box);

        box.AddChild(CreateTitle());
        _detail = CreateDetail();
        box.AddChild(_detail);

        AddRecoveryActions(box);
    }

    private static CanvasLayer CreateLayer()
        => new()
        {
            Name = NodeName,
            Layer = CanvasLayerIndex,
        };

    private static VBoxContainer CreateContainer()
    {
        var box = new VBoxContainer
        {
            Position = ContainerPosition,
            CustomMinimumSize = ContainerMinimumSize,
        };
        box.AddThemeConstantOverride(ThemeSeparation, ContainerSeparation);
        return box;
    }

    private static Label CreateTitle()
    {
        var title = new Label
        {
            Text = "Game is starting.",
        };
        title.AddThemeFontSizeOverride(ThemeFontSize, TitleFontSize);
        title.AddThemeColorOverride(ThemeFontColor, TitleColor);
        return title;
    }

    private static Label CreateDetail()
    {
        var detail = new Label
        {
            Text = "If this screen does not change, copy the raw error log, export diagnostics, or restart with safe launch. These controls stay visible for several minutes.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        detail.AddThemeFontSizeOverride(ThemeFontSize, DetailFontSize);
        detail.AddThemeColorOverride(ThemeFontColor, DetailColor);
        return detail;
    }

    private void AddRecoveryActions(VBoxContainer box)
    {
        AddButton(box, "RETURN TO LAUNCHER", AndroidGodotAppBridge.RestartApp);
        AddButton(box, "RESTART WITH SAFE LAUNCH", RestartWithSafeLaunch);
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
