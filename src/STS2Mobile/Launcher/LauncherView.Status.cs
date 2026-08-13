using System;
using Godot;

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
        var message = _profile.Compact
            ? LauncherPortalStatusFormatter.CompactMessageFor(text)
            : fullMessage;
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
        _secondaryStatusSeverityLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            color
        );
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
        if (!_profile.Compact
            || string.Equals(
                _compactStatusShortMessage,
                _compactStatusFullMessage,
                StringComparison.Ordinal
            ))
        {
            return;
        }

        _parent.GetViewport()?.GuiReleaseFocus();
        _compactStatusExpanded = !_compactStatusExpanded;
        _statusLabel.Text = _compactStatusExpanded
            ? _compactStatusFullMessage
            : _compactStatusShortMessage;
        ApplyCompactStatusDetailLayout();
    }

    private void ApplyCompactStatusDetailLayout()
    {
        var hasFullDetails = !string.Equals(
            _compactStatusShortMessage,
            _compactStatusFullMessage,
            StringComparison.Ordinal
        );
        var expanded = _compactStatusExpanded;
        _statusLabel.AutowrapMode = expanded
            ? TextServer.AutowrapMode.WordSmart
            : TextServer.AutowrapMode.Off;
        _statusLabel.ClipText = !expanded;
        _compactStatusDetailsButton.Disabled = !hasFullDetails;
        _compactStatusDetailsButton.MouseDefaultCursorShape = hasFullDetails
            ? Control.CursorShape.PointingHand
            : Control.CursorShape.Arrow;
        _compactStatusDetailsCueLabel.Visible = hasFullDetails;
        _compactStatusDetailsCueLabel.Text = expanded ? "Hide" : "Details";
    }

    private static bool ShouldAutoExpandCompactStatusDetails(LauncherStatusSeverity severity)
        => severity is LauncherStatusSeverity.Warning or LauncherStatusSeverity.Error;

    private void UpdateStatusVisibility()
    {
        var home = _destination == LauncherDestination.Home;
        var needsAttention = _currentStatusSeverity is LauncherStatusSeverity.Working
                or LauncherStatusSeverity.Warning
                or LauncherStatusSeverity.Error;
        _statusCapsule.Visible = home && needsAttention;
        _secondaryStatusBanner.Visible = !home && needsAttention;
    }
}
