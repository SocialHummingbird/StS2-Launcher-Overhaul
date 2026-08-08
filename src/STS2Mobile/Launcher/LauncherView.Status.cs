using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private string _compactStatusShortMessage = "";
    private string _compactStatusFullMessage = "";
    private string _compactStatusPhase = "Status";
    private bool _compactStatusExpanded;

    internal void SetStatus(string text)
    {
        var phase = LauncherPortalStatusFormatter.PhaseFor(text);
        var color = LauncherPortalStatusFormatter.ColorFor(phase);
        var fullMessage = LauncherPortalStatusFormatter.MessageFor(text);
        var message = _profile.Compact
            ? LauncherPortalStatusFormatter.CompactMessageFor(text)
            : fullMessage;
        _compactStatusShortMessage = message;
        _compactStatusFullMessage = fullMessage;
        _compactStatusPhase = phase;
        _compactStatusExpanded = ShouldAutoExpandCompactStatusDetails(phase);
        _statusPhaseLabel.Text = phase;
        _statusPhaseLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        _statusActionLabel.Text = LauncherPortalStatusFormatter.ActionFor(text);
        _statusAccent.Color = color;
        _statusLabel.Text = _compactStatusExpanded ? fullMessage : message;
        _statusLabel.TooltipText = fullMessage;
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

    private static bool ShouldAutoExpandCompactStatusDetails(string phase)
        => string.Equals(phase, "Attention", StringComparison.Ordinal);
}
