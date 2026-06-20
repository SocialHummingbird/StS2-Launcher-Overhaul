using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

// Owns launcher view references and constructor wiring.
internal sealed partial class LauncherView
{
    private LoginSection Login { get; }
    private CodeSection Code { get; }
    private DownloadSection Download { get; }
    private ActionSection Actions { get; }
    private ScrollContainer PrimaryScroll { get; }
    private Control FirstRunGuide { get; }
    private RichTextLabel Log { get; }
    private VBoxContainer DiagnosticsDrawer { get; }
    private Button DiagnosticsToggle { get; }

    private readonly Control _parent;
    private readonly StyledPanel _panel;
    private float _panelBaseY;
    private float _keyboardOffset;
    private readonly float _scale;
    private readonly LauncherLayoutProfile _profile;
    private readonly StyledLabel _statusPhaseLabel;
    private readonly StyledLabel _statusActionLabel;
    private readonly StyledLabel _statusLabel;
    private readonly Button _compactStatusDetailsButton;
    private readonly StyledLabel _compactStatusDetailsCueLabel;
    private readonly ColorRect _statusAccent;
    private readonly StyledLabel[] _workflowStepNumberLabels;
    private readonly StyledLabel[] _workflowStepLabels;
    private readonly StyledLabel[] _workflowStepDetailLabels;
    private readonly ColorRect[] _workflowStepAccents;
    private readonly Button[] _workflowStepButtons;
    private readonly GridContainer _compactStatusHeadline;
    private readonly PanelContainer _compactStatusPhasePanel;
    private readonly GridContainer _compactStickyTaskHeader;
    private readonly Control _compactWorkflowStrip;
    private readonly Button _compactCurrentTaskButton;
    private Control _compactCurrentTaskTarget;
    private Control _compactScrollAnchorTarget;
    private Control _keyboardFocusScrollTarget;
    private float _keyboardFocusScrollOffset = -1f;

    internal LauncherView(Control parent, LauncherLayoutProfile profile)
    {
        var dismissKeyboard = new Action<InputEvent>(DismissKeyboard);
        var shell = BuildShell(parent, profile, dismissKeyboard);
        _parent = parent;
        _panel = shell.Panel;
        _panelBaseY = shell.Panel.Position.Y;
        _scale = profile.Scale;
        _profile = profile;
        var primary = BuildPrimaryColumn(profile, shell.Content);
        _statusPhaseLabel = primary.StatusPhase;
        _statusActionLabel = primary.StatusAction;
        _statusLabel = primary.Status;
        _compactStatusDetailsButton = primary.CompactStatusDetailsButton;
        _compactStatusDetailsCueLabel = primary.CompactStatusDetailsCue;
        _statusAccent = primary.StatusAccent;
        _workflowStepNumberLabels = primary.WorkflowStepNumberLabels;
        _workflowStepLabels = primary.WorkflowStepLabels;
        _workflowStepDetailLabels = primary.WorkflowStepDetailLabels;
        _workflowStepAccents = primary.WorkflowStepAccents;
        _workflowStepButtons = primary.WorkflowStepButtons;
        _compactStatusHeadline = primary.CompactStatusHeadline;
        _compactStatusPhasePanel = primary.CompactStatusPhasePanel;
        _compactStickyTaskHeader = primary.CompactStickyTaskHeader;
        _compactWorkflowStrip = primary.CompactWorkflowStrip;
        _compactCurrentTaskButton = primary.CompactCurrentTaskButton;
        PrimaryScroll = primary.PrimaryScroll;
        FirstRunGuide = primary.FirstRunGuide;
        Login = primary.Login;
        Code = primary.Code;
        Download = primary.Download;
        Actions = primary.Actions;
        var diagnosticsRoot = profile.Compact
            ? primary.CompactDiagnosticsHost
            : shell.Content;
        var diagnostics = BuildLogColumn(profile, diagnosticsRoot, dismissKeyboard);
        Log = diagnostics.Log;
        DiagnosticsDrawer = diagnostics.Drawer;
        DiagnosticsToggle = diagnostics.Toggle;
        _compactCurrentTaskTarget = FirstRunGuide;
        _compactScrollAnchorTarget = FirstRunGuide;
        WireCompactCurrentTaskNavigation();
        WireCompactWorkflowStepNavigation();
        WireCompactStatusDetailToggle();
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("Start here", FirstRunGuide, "Setup guide");
    }
}
