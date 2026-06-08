using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CopyRawErrorLogButton = "COPY RAW ERROR LOG";
    private const string ExportDiagnosticsButton = "EXPORT STARTUP DIAGNOSTICS";
    private const string HideControlsButton = "HIDE RECOVERY CONTROLS";
    private const string RestartSafeLaunchButton = "RESTART WITH SAFE LAUNCH";
    private const string ReturnToLauncherButton = "RETURN TO LAUNCHER";

    private readonly struct RecoveryButtonSpec
    {
        private RecoveryButtonSpec(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        private string Label { get; }
        private Action Run { get; }

        private Button CreateButton()
        {
            var button = new Button
            {
                Text = Label,
                CustomMinimumSize = ButtonMinimumSize,
            };
            button.Pressed += Run;
            return button;
        }

        internal static Button CreateButton(string label, Action run)
            => new RecoveryButtonSpec(label, run).CreateButton();
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
        => CreateLabel(
            "Game is starting...",
            TitleFontSize,
            TitleColor
        );

    private static Label CreateDetail()
    {
        var detail = CreateLabel(
            "If this screen does not change, copy the raw error log, export diagnostics, or restart with safe launch. These controls hide automatically after a successful startup.",
            DetailFontSize,
            DetailColor
        );
        detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return detail;
    }

    private static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
        };
        label.AddThemeFontSizeOverride(ThemeFontSize, fontSize);
        label.AddThemeColorOverride(ThemeFontColor, color);
        return label;
    }

    private void AddRecoveryActions(VBoxContainer box)
    {
        foreach (var action in RecoveryButtons())
            box.AddChild(action);
    }

    private Button[] RecoveryButtons()
        => new[]
        {
            RecoveryButtonSpec.CreateButton(ReturnToLauncherButton, AndroidGodotAppBridge.RestartApp),
            RecoveryButtonSpec.CreateButton(RestartSafeLaunchButton, RestartWithSafeLaunch),
            RecoveryButtonSpec.CreateButton(ExportDiagnosticsButton, ExportDiagnostics),
            RecoveryButtonSpec.CreateButton(CopyRawErrorLogButton, CopyRawErrorLog),
            RecoveryButtonSpec.CreateButton(HideControlsButton, HideRecoveryControls),
        };

    private void HideRecoveryControls()
        => Layer.QueueFree();
}
