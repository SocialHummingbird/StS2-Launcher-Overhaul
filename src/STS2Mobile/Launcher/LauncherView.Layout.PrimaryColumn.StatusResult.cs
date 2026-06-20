using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewPrimaryStatus
{
    internal LauncherViewPrimaryStatus(
        StyledLabel phase,
        StyledLabel action,
        StyledLabel message,
        ColorRect accent,
        Control capsule,
        Button compactDetailButton,
        StyledLabel compactDetailCue,
        GridContainer compactHeadline,
        PanelContainer compactPhasePanel
    )
    {
        Phase = phase;
        Action = action;
        Message = message;
        Accent = accent;
        Capsule = capsule;
        CompactDetailButton = compactDetailButton;
        CompactDetailCue = compactDetailCue;
        CompactHeadline = compactHeadline;
        CompactPhasePanel = compactPhasePanel;
    }

    internal StyledLabel Phase { get; }
    internal StyledLabel Action { get; }
    internal StyledLabel Message { get; }
    internal ColorRect Accent { get; }
    internal Control Capsule { get; }
    internal Button CompactDetailButton { get; }
    internal StyledLabel CompactDetailCue { get; }
    internal GridContainer CompactHeadline { get; }
    internal PanelContainer CompactPhasePanel { get; }
}
