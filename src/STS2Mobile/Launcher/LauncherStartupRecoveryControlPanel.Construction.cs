using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryButton
    {
        internal RecoveryButton(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        private string Label { get; }
        private Action Run { get; }

        internal Button CreateControl()
        {
            var button = new Button
            {
                Text = Label,
                CustomMinimumSize = ButtonMinimumSize,
            };
            button.Pressed += Run;
            return button;
        }
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
        yield return new RecoveryButton("RETURN TO LAUNCHER", AndroidGodotAppBridge.RestartApp);
        yield return new RecoveryButton("RESTART WITH SAFE LAUNCH", RestartWithSafeLaunch);
        yield return new RecoveryButton("EXPORT STARTUP DIAGNOSTICS", ExportDiagnostics);
        yield return new RecoveryButton("COPY RAW ERROR LOG", CopyRawErrorLog);
        yield return new RecoveryButton("HIDE RECOVERY CONTROLS", () => Layer.QueueFree());
    }

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private static void AddButton(VBoxContainer box, RecoveryButton action)
        => box.AddChild(action.CreateControl());
}
