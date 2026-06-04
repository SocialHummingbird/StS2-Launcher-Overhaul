using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryButtonSpec
    {
        internal RecoveryButtonSpec(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        private string Label { get; }
        private Action Run { get; }

        internal void AddTo(VBoxContainer box)
        {
            var button = new Button
            {
                Text = Label,
                CustomMinimumSize = ButtonMinimumSize,
            };
            button.Pressed += Run;
            box.AddChild(button);
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
            action.AddTo(box);
    }

    private IEnumerable<RecoveryButtonSpec> RecoveryActions()
    {
        yield return new RecoveryButtonSpec(
            "RETURN TO LAUNCHER",
            AndroidGodotAppBridge.RestartApp
        );
        yield return new RecoveryButtonSpec(
            "RESTART WITH SAFE LAUNCH",
            RestartWithSafeLaunch
        );
        yield return new RecoveryButtonSpec(
            "EXPORT STARTUP DIAGNOSTICS",
            ExportDiagnostics
        );
        yield return new RecoveryButtonSpec("COPY RAW ERROR LOG", CopyRawErrorLog);
        yield return new RecoveryButtonSpec(
            "HIDE RECOVERY CONTROLS",
            () => Layer.QueueFree()
        );
    }
}
