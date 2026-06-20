using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CopyRawErrorLogButton = "COPY RAW LOG (REVIEW BEFORE SHARING)";
    private const string CopyRawErrorLogCompactButton = "COPY RAW LOG";
    private const string ExportDiagnosticsButton = "EXPORT STARTUP DIAGNOSTICS";
    private const string ExportDiagnosticsCompactButton = "EXPORT DIAGNOSTICS";
    private const string HideControlsButton = "HIDE RECOVERY CONTROLS";
    private const string HideControlsCompactButton = "HIDE RECOVERY";
    private const string RestartSafeLaunchButton = "RESTART WITH SAFE LAUNCH";
    private const string RestartSafeLaunchCompactButton = "RESTART SAFE LAUNCH";
    private const string ReturnToLauncherButton = "RETURN TO LAUNCHER";
    private const string ReturnToLauncherCompactButton = "RESTART APP";

    private readonly struct RecoveryButtonSpec
    {
        private RecoveryButtonSpec(string label, Action run)
        {
            Label = label;
            Run = run;
        }

        private string Label { get; }
        private Action Run { get; }

        private Button CreateButton(float scale, Vector2 minimumSize)
        {
            var button = new Button
            {
                Text = Label,
                CustomMinimumSize = minimumSize,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride(
                ThemeFontSize,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonDefaultFontSize)
            );
            LauncherButtonStyles.ApplySupportAction(button, scale);
            button.Pressed += Run;
            return button;
        }

        internal static Button CreateButton(
            string label,
            Action run,
            float scale,
            Vector2 minimumSize
        )
            => new RecoveryButtonSpec(label, run).CreateButton(scale, minimumSize);
    }

    private LauncherStartupRecoveryControlPanel(Vector2 viewportSize)
    {
        var scale = LayoutScale(viewportSize);
        var compactCopy = UseCompactRecoveryCopy(viewportSize);
        Layer = CreateLayer();

        var scroll = CreateScrollContainer();
        Layer.AddChild(scroll);

        var frame = CreateFrame(viewportSize);
        scroll.AddChild(frame);

        var box = CreateContainer(viewportSize);
        frame.AddChild(box);

        box.AddChild(CreateTitle(scale));
        _detail = CreateDetail(scale, compactCopy);
        box.AddChild(_detail);

        AddRecoveryActions(box, scale, ButtonMinimumSize(viewportSize, scale), compactCopy);
    }

    private static CanvasLayer CreateLayer()
        => new()
        {
            Name = NodeName,
            Layer = CanvasLayerIndex,
        };

    private static ScrollContainer CreateScrollContainer()
    {
        var scroll = new ScrollContainer
        {
            FollowFocus = true,
        };
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return scroll;
    }

    private static MarginContainer CreateFrame(Vector2 viewportSize)
    {
        var margin = RecoveryMargin(viewportSize);
        var frame = new MarginContainer();
        frame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        frame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        frame.AddThemeConstantOverride("margin_left", margin);
        frame.AddThemeConstantOverride("margin_right", margin);
        frame.AddThemeConstantOverride("margin_top", RecoveryTopMargin(viewportSize));
        frame.AddThemeConstantOverride("margin_bottom", margin);
        return frame;
    }

    private static VBoxContainer CreateContainer(Vector2 viewportSize)
    {
        var margin = RecoveryMargin(viewportSize);
        var width = Math.Min(ContainerMaxWidth, Math.Max(320f, viewportSize.X - (margin * 2f)));
        var box = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(width, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        box.AddThemeConstantOverride(ThemeSeparation, ContainerSeparation);
        return box;
    }

    private static Vector2 ButtonMinimumSize(Vector2 viewportSize, float scale)
    {
        var width = Math.Min(ButtonMaxWidth, Math.Max(280f, viewportSize.X - (ContainerMargin * 2f)));
        return new Vector2(width, LauncherComponentTheme.ScaleInt(scale, (int)ButtonHeight));
    }

    private static bool UseCompactRecoveryCopy(Vector2 viewportSize)
        => OperatingSystem.IsAndroid() || Math.Min(viewportSize.X, viewportSize.Y) < 720f;

    private static int RecoveryMargin(Vector2 viewportSize)
        => LauncherComponentTheme.ScaleInt(LayoutScale(viewportSize), OperatingSystem.IsAndroid() ? 16 : (int)ContainerMargin);

    private static int RecoveryTopMargin(Vector2 viewportSize)
        => LauncherComponentTheme.ScaleInt(
            LayoutScale(viewportSize),
            OperatingSystem.IsAndroid() ? 18 : (int)Math.Min(ContainerTop, Math.Max(16f, viewportSize.Y * 0.06f))
        );

    private static Label CreateTitle(float scale)
        => CreateLabel(
            "Game is starting...",
            LauncherComponentTheme.ScaleInt(scale, TitleFontSize),
            TitleColor
        );

    private static Label CreateDetail(float scale, bool compact)
    {
        var detail = CreateLabel(
            compact
                ? "If startup stalls, export diagnostics, copy the raw log for local review, or restart with safe launch. Review raw logs before sharing."
                : "If this screen does not change, export diagnostics, copy the raw error log for local review, or restart with safe launch. Raw logs can contain identifying data; review/redact before sharing. These controls hide automatically after a successful startup.",
            LauncherComponentTheme.ScaleInt(scale, DetailFontSize),
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

    private void AddRecoveryActions(VBoxContainer box, float scale, Vector2 buttonMinimumSize, bool compactCopy)
    {
        foreach (var action in RecoveryButtons(scale, buttonMinimumSize, compactCopy))
            box.AddChild(action);
    }

    private Button[] RecoveryButtons(float scale, Vector2 buttonMinimumSize, bool compactCopy)
        => new[]
        {
            RecoveryButtonSpec.CreateButton(compactCopy ? ReturnToLauncherCompactButton : ReturnToLauncherButton, AndroidGodotAppBridge.RestartApp, scale, buttonMinimumSize),
            RecoveryButtonSpec.CreateButton(compactCopy ? RestartSafeLaunchCompactButton : RestartSafeLaunchButton, RestartWithSafeLaunch, scale, buttonMinimumSize),
            RecoveryButtonSpec.CreateButton(compactCopy ? ExportDiagnosticsCompactButton : ExportDiagnosticsButton, ExportDiagnostics, scale, buttonMinimumSize),
            RecoveryButtonSpec.CreateButton(compactCopy ? CopyRawErrorLogCompactButton : CopyRawErrorLogButton, CopyRawErrorLog, scale, buttonMinimumSize),
            RecoveryButtonSpec.CreateButton(compactCopy ? HideControlsCompactButton : HideControlsButton, HideRecoveryControls, scale, buttonMinimumSize),
        };

    private void HideRecoveryControls()
        => Layer.QueueFree();
}
