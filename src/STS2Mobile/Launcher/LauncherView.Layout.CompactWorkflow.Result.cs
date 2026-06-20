using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewCompactWorkflowStrip
{
    internal LauncherViewCompactWorkflowStrip(
        Control strip,
        StyledLabel[] stepNumberLabels,
        StyledLabel[] stepLabels,
        StyledLabel[] stepDetailLabels,
        ColorRect[] stepAccents,
        Button[] stepButtons
    )
    {
        Strip = strip;
        StepNumberLabels = stepNumberLabels;
        StepLabels = stepLabels;
        StepDetailLabels = stepDetailLabels;
        StepAccents = stepAccents;
        StepButtons = stepButtons;
    }

    internal Control Strip { get; }
    internal StyledLabel[] StepNumberLabels { get; }
    internal StyledLabel[] StepLabels { get; }
    internal StyledLabel[] StepDetailLabels { get; }
    internal ColorRect[] StepAccents { get; }
    internal Button[] StepButtons { get; }
}

internal readonly struct LauncherViewCompactWorkflowStepCell
{
    internal LauncherViewCompactWorkflowStepCell(
        Button button,
        StyledLabel numberLabel,
        StyledLabel label,
        StyledLabel detailLabel,
        ColorRect accent
    )
    {
        Button = button;
        NumberLabel = numberLabel;
        Label = label;
        DetailLabel = detailLabel;
        Accent = accent;
    }

    internal Button Button { get; }
    internal StyledLabel NumberLabel { get; }
    internal StyledLabel Label { get; }
    internal StyledLabel DetailLabel { get; }
    internal ColorRect Accent { get; }
}
