using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewCompactWorkflowStepCell BuildCompactWorkflowStepCell(
        int index,
        float scale,
        int stepHeight
    )
    {
        var button = BuildCompactWorkflowStepButton(index, scale, stepHeight);

        var body = BuildCompactWorkflowStepBody(scale);
        button.AddChild(body);

        var labelRow = BuildCompactWorkflowLabelRow(scale);
        body.AddChild(labelRow);

        var numberLabel = BuildCompactWorkflowNumberLabel(index, scale);
        labelRow.AddChild(numberLabel);

        var label = BuildCompactWorkflowLabel(index, scale);
        labelRow.AddChild(label);

        var detail = BuildCompactWorkflowDetailLabel(index, scale);
        body.AddChild(detail);

        var accent = BuildCompactWorkflowAccent(scale);
        body.AddChild(accent);

        return new LauncherViewCompactWorkflowStepCell(
            button,
            numberLabel,
            label,
            detail,
            accent
        );
    }
}
