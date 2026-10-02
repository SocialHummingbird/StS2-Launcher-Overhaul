using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherView
{
    private string _compactStatusShortMessage = "";
    private string _compactStatusFullMessage = "";
    private LauncherStatusSeverity _currentStatusSeverity = LauncherStatusSeverity.Working;
    private bool _compactStatusExpanded;
    internal void SetStatus(string text, LauncherStatusSeverity severity)
    {
        var label = LauncherPortalStatusFormatter.LabelFor(severity);
        var color = LauncherPortalStatusFormatter.ColorFor(severity);
        var fullMessage = LauncherPortalStatusFormatter.MessageFor(text);
        var message = _profile.Compact ? LauncherPortalStatusFormatter.CompactMessageFor(text) : fullMessage;
        _compactStatusShortMessage = message;
        _compactStatusFullMessage = fullMessage;
        _currentStatusSeverity = severity;
        _compactStatusExpanded = ShouldAutoExpandCompactStatusDetails(severity);
        _statusPhaseLabel.Text = label;
        _statusPhaseLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        _statusAccent.Color = color;
        _statusLabel.Text = _compactStatusExpanded ? fullMessage : message;
        _statusLabel.TooltipText = fullMessage;
        _secondaryStatusSeverityLabel.Text = label;
        _secondaryStatusSeverityLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        _secondaryStatusMessageLabel.Text = fullMessage;
        _secondaryStatusMessageLabel.TooltipText = fullMessage;
        _secondaryStatusAccent.Color = color;
        UpdateStatusVisibility();
        if (_profile.Compact)
        {
            ApplyCompactStatusDetailLayout();
        }
    }

    private void WireCompactStatusDetailToggle()
    {
        if (!_profile.Compact)
            return;
        _compactStatusDetailsButton.Pressed += ToggleCompactStatusDetails;
    }

    private void ToggleCompactStatusDetails()
    {
        if (!_profile.Compact || string.Equals(_compactStatusShortMessage, _compactStatusFullMessage, StringComparison.Ordinal))
        {
            return;
        }

        _parent.GetViewport()?.GuiReleaseFocus();
        _compactStatusExpanded = !_compactStatusExpanded;
        _statusLabel.Text = _compactStatusExpanded ? _compactStatusFullMessage : _compactStatusShortMessage;
        ApplyCompactStatusDetailLayout();
    }

    private void ApplyCompactStatusDetailLayout()
    {
        var hasFullDetails = !string.Equals(_compactStatusShortMessage, _compactStatusFullMessage, StringComparison.Ordinal);
        var expanded = _compactStatusExpanded;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusLabel.ClipText = false;
        _compactStatusDetailsButton.Visible = hasFullDetails;
        _compactStatusDetailsButton.Text = expanded ? "Hide details" : "Show details";
        _compactStatusDetailsButton.AccessibilityName = _compactStatusDetailsButton.Text;
        _compactStatusDetailsButton.Disabled = !hasFullDetails;
        _compactStatusDetailsButton.MouseDefaultCursorShape = hasFullDetails ? Control.CursorShape.PointingHand : Control.CursorShape.Arrow;
        _compactStatusDetailsCueLabel.Visible = false;
        _compactStatusDetailsCueLabel.Text = expanded ? "Hide" : "Details";
    }

    private static bool ShouldAutoExpandCompactStatusDetails(LauncherStatusSeverity severity) => severity is LauncherStatusSeverity.Warning or LauncherStatusSeverity.Error;
    private void UpdateStatusVisibility()
    {
        var home = _destination == LauncherDestination.Home;
        var needsAttention = _currentStatusSeverity is LauncherStatusSeverity.Working or LauncherStatusSeverity.Warning or LauncherStatusSeverity.Error;
        _statusCapsule.Visible = home && needsAttention;
        _secondaryStatusBanner.Visible = !home && needsAttention;
    }

    private static LauncherViewSecondaryStatus BuildSecondaryStatusBanner(LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var panel = new PanelContainer
        {
            Name = "SecondaryStatusBanner",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var style = LauncherStyleBoxes.MakeFilled(new Color(0.025f, 0.04f, 0.05f, 0.86f), LauncherViewLayoutMetrics.ScaleInt(5, scale));
        style.BorderColor = new Color(0.08f, 0.24f, 0.28f, 0.6f);
        style.SetBorderWidthAll(System.Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(6, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(6, scale);
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, style);
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(8, scale));
        panel.AddChild(row);
        var accent = new ColorRect
        {
            CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(3, scale), 0),
        };
        row.AddChild(accent);
        var severity = new StyledLabel("Working", scale, fontSize: profile.Compact ? 12 : 11, align: HorizontalAlignment.Left)
        {
            Name = "SecondaryStatusSeverity",
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(severity);
        var message = new StyledLabel("Starting launcher...", scale, fontSize: profile.Compact ? 13 : 12, align: HorizontalAlignment.Left)
        {
            Name = "SecondaryStatusMessage",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(message);
        return new LauncherViewSecondaryStatus(panel, severity, message, accent);
    }

    private static (Control Capsule, Button CompactDetailButton, StyledLabel CompactDetailCue) BuildCompactStatusCapsule(StyledLabel statusPhaseLabel, StyledLabel statusLabel, ColorRect statusAccent, LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var panel = new PanelContainer();
        panel.Name = "GlobalStatusCapsule";
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildStatusStyle(scale, compact: true));
        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactStatusBodySeparation, scale));
        panel.AddChild(body);
        statusAccent.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactStatusAccentHeight, scale));
        body.AddChild(statusAccent);
        var headline = new GridContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var phasePanel = new PanelContainer();
        phasePanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        phasePanel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildStatusPhaseStyle(scale, compact: true));
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phasePanel.AddChild(statusPhaseLabel);
        headline.AddChild(phasePanel);
        headline.Columns = 1;
        phasePanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(headline);
        var detailButton = BuildCompactStatusDetailButton(scale);
        // Container-managed text contributes its wrapped height to the status panel.
        // A label anchored inside a Button can collapse to zero width on phones.
        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(statusLabel);
        var detailCue = BuildCompactStatusDetailCue(scale);
        detailCue.Visible = false;
        body.AddChild(detailCue);
        body.AddChild(detailButton);
        return (panel, detailButton, detailCue);
    }

    private const int CompactStatusBodySeparation = 5;
    private const int CompactStatusAccentHeight = 3;
    private const int CompactStatusPhaseHorizontalMargin = 7;
    private const int CompactStatusPhaseVerticalMargin = 3;
    private const int CompactStatusActionMinHeight = 24;
    private const int CompactStatusDetailHeight = 44;
    private const int CompactStatusDetailCueWidth = 62;
    private const int CompactStatusDetailCueFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactStatusDetailHorizontalMargin = 8;
    private const int CompactStatusDetailVerticalMargin = 5;
    private const int CompactStatusDetailRowGap = 6;
    private const int CompactStatusDetailRadius = 7;
    private static (Control Capsule, Button CompactDetailButton, StyledLabel CompactDetailCue) BuildStatusCapsule(StyledLabel statusPhaseLabel, StyledLabel statusLabel, ColorRect statusAccent, LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        if (profile.Compact)
            return BuildCompactStatusCapsule(statusPhaseLabel, statusLabel, statusAccent, profile);
        var panel = new PanelContainer();
        panel.Name = "GlobalStatusCapsule";
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildStatusStyle(scale, compact: false));
        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(8, scale));
        panel.AddChild(body);
        statusAccent.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(4, scale), LauncherViewLayoutMetrics.ScaleInt(30, scale));
        body.AddChild(statusAccent);
        var phasePanel = new PanelContainer();
        phasePanel.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(96, scale), 0);
        phasePanel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildStatusPhaseStyle(scale, compact: false));
        var phaseBody = new VBoxContainer();
        phaseBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        phaseBody.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(1, scale));
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phaseBody.AddChild(statusPhaseLabel);
        phasePanel.AddChild(phaseBody);
        body.AddChild(phasePanel);
        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(statusLabel);
        return (panel, null, null);
    }

    private static Button BuildCompactStatusDetailButton(float scale)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Show full launcher status",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHeight, scale)),
        };
        ApplyCompactStatusDetailButtonStyle(button, scale);
        return button;
    }

    private static HBoxContainer BuildCompactStatusDetailRow(StyledLabel statusLabel, float scale)
    {
        var detailRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        detailRow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        detailRow.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRowGap, scale));
        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusLabel.VerticalAlignment = VerticalAlignment.Center;
        statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        statusLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        statusLabel.ClipText = true;
        statusLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailRow.AddChild(statusLabel);
        return detailRow;
    }

    private static StyledLabel BuildCompactStatusDetailCue(float scale)
    {
        var detailCue = new StyledLabel("Details", scale, fontSize: CompactStatusDetailCueFontSize, align: HorizontalAlignment.Center);
        detailCue.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailCueWidth, scale), 0);
        detailCue.VerticalAlignment = VerticalAlignment.Center;
        detailCue.MouseFilter = Control.MouseFilterEnum.Ignore;
        detailCue.ClipText = true;
        detailCue.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailCue.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.CyanAccent);
        return detailCue;
    }

    private static void ApplyCompactStatusDetailButtonStyle(Button button, float scale)
    {
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildCompactStatusDetailButtonStyle(scale, LauncherComponentTheme.ButtonNormal, new Color(0.05f, 0.34f, 0.42f, 0.4f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, BuildCompactStatusDetailButtonStyle(scale, LauncherComponentTheme.ButtonNormal, new Color(0.06f, 0.54f, 0.62f, 0.58f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, BuildCompactStatusDetailButtonStyle(scale, LauncherComponentTheme.ButtonNormal, new Color(0.95f, 0.42f, 0.08f, 0.68f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateDisabled, BuildCompactStatusDetailButtonStyle(scale, LauncherComponentTheme.ButtonNormal, new Color(0.05f, 0.16f, 0.2f, 0.24f)));
    }

    private static StyleBoxFlat BuildCompactStatusDetailButtonStyle(float scale, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(body, LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRadius, scale));
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonNormal, LauncherViewLayoutMetrics.ScaleInt(8, scale));
        style.BorderColor = LauncherComponentTheme.ButtonHover;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 8, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusPhaseStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonNormal, LauncherViewLayoutMetrics.ScaleInt(7, scale));
        style.BorderColor = LauncherComponentTheme.ButtonHover;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        return style;
    }

    internal LauncherPreparationOverlay LockPreparationInteraction()
    {
        var overlay = new LauncherPreparationOverlay
        {
            Name = "LaunchPreparation",
            Layer = 2000,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        var shade = new ColorRect
        {
            Color = LauncherComponentTheme.DialogOverlay,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var center = BuildDialogCenter();
        var box = BuildDialogBox(_scale);
        overlay.Message = BuildDialogMessage("Preparing game files…", _profile);
        box.AddChild(overlay.Message);
        center.AddChild(box);
        shade.AddChild(center);
        overlay.AddChild(shade);
        _parent.GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        _parent.AddChild(overlay);
        overlay.SetProcessInput(true);
        return overlay;
    }
}

internal readonly struct LauncherViewSecondaryStatus
{
    internal LauncherViewSecondaryStatus(Control banner, StyledLabel severity, StyledLabel message, ColorRect accent)
    {
        Banner = banner;
        Severity = severity;
        Message = message;
        Accent = accent;
    }

    internal Control Banner { get; }
    internal StyledLabel Severity { get; }
    internal StyledLabel Message { get; }
    internal ColorRect Accent { get; }
}
