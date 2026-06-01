using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryButton
    {
        private RecoveryButton(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        internal string Label { get; }
        internal Action Run { get; }

        internal static RecoveryButton Create(string label, Action run)
            => new(label, run);
    }

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
        foreach (var action in RecoveryActions())
            AddButton(box, action);
    }

    private IEnumerable<RecoveryButton> RecoveryActions()
    {
        yield return RecoveryButton.Create("RETURN TO LAUNCHER", AndroidGodotAppBridge.RestartApp);
        yield return RecoveryButton.Create("RESTART WITH SAFE LAUNCH", RestartWithSafeLaunch);
        yield return RecoveryButton.Create("EXPORT STARTUP DIAGNOSTICS", ExportDiagnostics);
        yield return RecoveryButton.Create("COPY RAW ERROR LOG", CopyRawErrorLog);
        yield return RecoveryButton.Create("HIDE RECOVERY CONTROLS", () => Layer.QueueFree());
    }

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private static void AddButton(VBoxContainer box, RecoveryButton action)
    {
        var button = new Button
        {
            Text = action.Label,
            CustomMinimumSize = ButtonMinimumSize,
        };
        button.Pressed += action.Run;
        box.AddChild(button);
    }
}
