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
            "Game is starting.",
            TitleFontSize,
            TitleColor
        );

    private static Label CreateDetail()
    {
        var detail = CreateLabel(
            "If this screen does not change, copy the raw error log, export diagnostics, or restart with safe launch. These controls stay visible for several minutes.",
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
        AddRecoveryButton(box, ReturnToLauncherButton, AndroidGodotAppBridge.RestartApp);
        AddRecoveryButton(box, RestartSafeLaunchButton, RestartWithSafeLaunch);
        AddRecoveryButton(box, ExportDiagnosticsButton, ExportDiagnostics);
        AddRecoveryButton(box, CopyRawErrorLogButton, CopyRawErrorLog);
        AddRecoveryButton(box, HideControlsButton, HideRecoveryControls);
    }

    private static void AddRecoveryButton(
        VBoxContainer box,
        string label,
        Action run
    )
    {
        var button = new Button
        {
            Text = label,
            CustomMinimumSize = ButtonMinimumSize,
        };
        button.Pressed += run;
        box.AddChild(button);
    }

    private void HideRecoveryControls()
        => Layer.QueueFree();
}
