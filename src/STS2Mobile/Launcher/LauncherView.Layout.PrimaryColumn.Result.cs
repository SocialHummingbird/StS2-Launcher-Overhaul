using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewPrimaryColumn
{
    internal LauncherViewPrimaryColumn(
        StyledLabel statusPhase,
        StyledLabel statusAction,
        StyledLabel status,
        Button compactStatusDetailsButton,
        StyledLabel compactStatusDetailsCue,
        ColorRect statusAccent,
        StyledLabel[] workflowStepNumberLabels,
        StyledLabel[] workflowStepLabels,
        StyledLabel[] workflowStepDetailLabels,
        ColorRect[] workflowStepAccents,
        Button[] workflowStepButtons,
        GridContainer compactStatusHeadline,
        PanelContainer compactStatusPhasePanel,
        GridContainer compactStickyTaskHeader,
        Control compactWorkflowStrip,
        Button compactCurrentTaskButton,
        ScrollContainer primaryScroll,
        Control firstRunGuide,
        LoginSection login,
        CodeSection code,
        DownloadSection download,
        ActionSection actions,
        VBoxContainer compactDiagnosticsHost
    )
    {
        StatusPhase = statusPhase;
        StatusAction = statusAction;
        Status = status;
        CompactStatusDetailsButton = compactStatusDetailsButton;
        CompactStatusDetailsCue = compactStatusDetailsCue;
        StatusAccent = statusAccent;
        WorkflowStepNumberLabels = workflowStepNumberLabels;
        WorkflowStepLabels = workflowStepLabels;
        WorkflowStepDetailLabels = workflowStepDetailLabels;
        WorkflowStepAccents = workflowStepAccents;
        WorkflowStepButtons = workflowStepButtons;
        CompactStatusHeadline = compactStatusHeadline;
        CompactStatusPhasePanel = compactStatusPhasePanel;
        CompactStickyTaskHeader = compactStickyTaskHeader;
        CompactWorkflowStrip = compactWorkflowStrip;
        CompactCurrentTaskButton = compactCurrentTaskButton;
        PrimaryScroll = primaryScroll;
        FirstRunGuide = firstRunGuide;
        Login = login;
        Code = code;
        Download = download;
        Actions = actions;
        CompactDiagnosticsHost = compactDiagnosticsHost;
    }

    internal StyledLabel StatusPhase { get; }
    internal StyledLabel StatusAction { get; }
    internal StyledLabel Status { get; }
    internal Button CompactStatusDetailsButton { get; }
    internal StyledLabel CompactStatusDetailsCue { get; }
    internal ColorRect StatusAccent { get; }
    internal StyledLabel[] WorkflowStepNumberLabels { get; }
    internal StyledLabel[] WorkflowStepLabels { get; }
    internal StyledLabel[] WorkflowStepDetailLabels { get; }
    internal ColorRect[] WorkflowStepAccents { get; }
    internal Button[] WorkflowStepButtons { get; }
    internal GridContainer CompactStatusHeadline { get; }
    internal PanelContainer CompactStatusPhasePanel { get; }
    internal GridContainer CompactStickyTaskHeader { get; }
    internal Control CompactWorkflowStrip { get; }
    internal Button CompactCurrentTaskButton { get; }
    internal ScrollContainer PrimaryScroll { get; }
    internal Control FirstRunGuide { get; }
    internal LoginSection Login { get; }
    internal CodeSection Code { get; }
    internal DownloadSection Download { get; }
    internal ActionSection Actions { get; }
    internal VBoxContainer CompactDiagnosticsHost { get; }
}
