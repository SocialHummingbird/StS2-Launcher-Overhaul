using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CompactRecoveryButtonBodyName = "CompactRecoveryButtonBody";
    private const string CompactRecoveryButtonTitleName = "CompactRecoveryButtonTitle";
    private const string CompactRecoveryButtonDetailName = "CompactRecoveryButtonDetail";
    private const int CompactRecoveryButtonTitleFontSize = 16;
    private const int CompactRecoveryButtonDetailFontSize = 12;
    private const int CompactRecoveryButtonHorizontalMargin = 8;
    private const int CompactRecoveryButtonVerticalMargin = 6;

    private const string CopyRawErrorLogButton = "Copy Launcher Log (Review First)";
    private const string ExportDiagnosticsButton = "Create Startup Help Report";
    private const string HideControlsButton = "Hide Recovery Controls";
    private const string RestartSafeLaunchButton = "Restart with Safe Launch";
    private const string ReturnToLauncherButton = "Return to Launcher";

    private readonly struct RecoveryButtonSpec
    {
        private RecoveryButtonSpec(string label, string detail, Action run)
        {
            Label = label;
            Detail = detail;
            Run = run;
        }

        private string Label { get; }
        private string Detail { get; }
        private Action Run { get; }

        private Button CreateButton(float scale, Vector2 minimumSize, bool structured)
        {
            var button = new Button
            {
                Text = structured ? "" : Label,
                CustomMinimumSize = minimumSize,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            button.AddThemeFontSizeOverride(
                ThemeFontSize,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonDefaultFontSize)
            );
            LauncherButtonStyles.ApplySupportAction(button, scale);
            if (structured)
                AddCompactRecoveryButtonLabels(button, scale, Label, Detail);
            button.Pressed += Run;
            return button;
        }

        internal static Button CreateButton(
            string label,
            string detail,
            Action run,
            float scale,
            Vector2 minimumSize,
            bool compactCopy
        )
            => new RecoveryButtonSpec(label, detail, run)
                .CreateButton(scale, minimumSize, compactCopy && !string.IsNullOrWhiteSpace(detail));
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
                ? "If startup stalls, restart the app, try Safe Start, or create a help report. Review logs before sharing."
                : "If this screen does not change, create a help report, copy the launcher log for local review, or restart with safe launch. Logs can contain identifying data; review/redact before sharing. These controls hide automatically after a successful startup.",
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
            RecoveryButtonSpec.CreateButton(compactCopy ? "Restart App" : ReturnToLauncherButton, "Open launcher", AndroidGodotAppBridge.RestartApp, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Safe Start" : RestartSafeLaunchButton, "Cloud off", RestartWithSafeLaunch, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Help Report" : ExportDiagnosticsButton, "Share details", ExportDiagnostics, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Copy Log" : CopyRawErrorLogButton, "Review first", CopyRawErrorLog, scale, buttonMinimumSize, compactCopy),
            RecoveryButtonSpec.CreateButton(compactCopy ? "Hide Help" : HideControlsButton, "Keep waiting", HideRecoveryControls, scale, buttonMinimumSize, compactCopy),
        };

    private static void AddCompactRecoveryButtonLabels(
        Button button,
        float scale,
        string titleText,
        string detailText
    )
    {
        var body = new VBoxContainer
        {
            Name = CompactRecoveryButtonBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonHorizontalMargin);
        body.OffsetRight = -LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonHorizontalMargin);
        body.OffsetTop = LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonVerticalMargin);
        body.OffsetBottom = -LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonVerticalMargin);
        body.AddThemeConstantOverride(ThemeSeparation, 0);

        var title = CreateStructuredButtonLabel(
            CompactRecoveryButtonTitleName,
            titleText,
            scale,
            CompactRecoveryButtonTitleFontSize,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = CreateStructuredButtonLabel(
            CompactRecoveryButtonDetailName,
            detailText,
            scale,
            CompactRecoveryButtonDetailFontSize,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
    }

    private static Label CreateStructuredButtonLabel(
        string name,
        string text,
        float scale,
        int fontSize,
        Color color
    )
    {
        var label = new StyledLabel(
            text,
            scale,
            fontSize: fontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        label.AddThemeColorOverride(ThemeFontColor, color);
        return label;
    }

    private void HideRecoveryControls()
        => Layer.QueueFree();
}
