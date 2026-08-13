using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewPrimaryStatus
{
    internal LauncherViewPrimaryStatus(
        StyledLabel phase,
        StyledLabel message,
        ColorRect accent,
        Control capsule,
        Button compactDetailButton,
        StyledLabel compactDetailCue
    )
    {
        Phase = phase;
        Message = message;
        Accent = accent;
        Capsule = capsule;
        CompactDetailButton = compactDetailButton;
        CompactDetailCue = compactDetailCue;
    }

    internal StyledLabel Phase { get; }
    internal StyledLabel Message { get; }
    internal ColorRect Accent { get; }
    internal Control Capsule { get; }
    internal Button CompactDetailButton { get; }
    internal StyledLabel CompactDetailCue { get; }
}
