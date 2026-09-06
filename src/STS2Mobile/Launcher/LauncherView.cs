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
    private VBoxContainer HomeSections { get; }
    private ScrollContainer PrimaryScroll { get; }
    private Control FirstRunGuide { get; }
    private RichTextLabel Log { get; }
    private VBoxContainer DiagnosticsDrawer { get; }
    private Button DiagnosticsToggle { get; }

    private readonly Control _parent;
    private readonly StyledPanel _panel;
    internal Node LaunchLifetimeHost => _panel;
    private readonly ColorRect _androidCompositionRefresh;
    private float _panelBaseY;
    private float _keyboardOffset;
    private readonly float _scale;
    private readonly LauncherLayoutProfile _profile;
    private readonly StyledLabel _statusPhaseLabel;
    private readonly StyledLabel _statusLabel;
    private readonly Control _statusCapsule;
    private readonly Control _secondaryStatusBanner;
    private readonly StyledLabel _secondaryStatusSeverityLabel;
    private readonly StyledLabel _secondaryStatusMessageLabel;
    private readonly ColorRect _secondaryStatusAccent;
    private readonly Button _compactStatusDetailsButton;
    private readonly StyledLabel _compactStatusDetailsCueLabel;
    private readonly ColorRect _statusAccent;
    private readonly StyledLabel[] _workflowStepNumberLabels;
    private readonly StyledLabel[] _workflowStepLabels;
    private readonly StyledLabel[] _workflowStepDetailLabels;
    private readonly ColorRect[] _workflowStepAccents;
    private readonly Button[] _workflowStepButtons;
    private readonly GridContainer _compactStickyTaskHeader;
    private readonly Control _compactWorkflowStrip;
    private readonly Button _compactCurrentTaskButton;
    private readonly MarginContainer _destinationNavigationFrame;
    private readonly Control _destinationSafeAreaSpacer;
    private readonly Button[] _destinationButtons;
    private Vector4 _systemSafeAreaInsets = new(-1, -1, -1, -1);
    private int _androidCompositionRefreshFrames;
    private LauncherDestination _destination;
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
        _androidCompositionRefresh = shell.AndroidCompositionRefresh;
        _panelBaseY = shell.Panel.Position.Y;
        _scale = profile.Scale;
        _profile = profile;
        var primary = BuildPrimaryColumn(profile, shell.Content);
        _statusPhaseLabel = primary.StatusPhase;
        _statusLabel = primary.Status;
        _statusCapsule = primary.StatusCapsule;
        _secondaryStatusBanner = primary.SecondaryStatusBanner;
        _secondaryStatusSeverityLabel = primary.SecondaryStatusSeverity;
        _secondaryStatusMessageLabel = primary.SecondaryStatusMessage;
        _secondaryStatusAccent = primary.SecondaryStatusAccent;
        _compactStatusDetailsButton = primary.CompactStatusDetailsButton;
        _compactStatusDetailsCueLabel = primary.CompactStatusDetailsCue;
        _statusAccent = primary.StatusAccent;
        _workflowStepNumberLabels = primary.WorkflowStepNumberLabels;
        _workflowStepLabels = primary.WorkflowStepLabels;
        _workflowStepDetailLabels = primary.WorkflowStepDetailLabels;
        _workflowStepAccents = primary.WorkflowStepAccents;
        _workflowStepButtons = primary.WorkflowStepButtons;
        _compactStickyTaskHeader = primary.CompactStickyTaskHeader;
        _compactWorkflowStrip = primary.CompactWorkflowStrip;
        _compactCurrentTaskButton = primary.CompactCurrentTaskButton;
        PrimaryScroll = primary.PrimaryScroll;
        FirstRunGuide = primary.FirstRunGuide;
        HomeSections = primary.HomeSections;
        Login = primary.Login;
        Code = primary.Code;
        Download = primary.Download;
        Actions = primary.Actions;
        Actions.HomeHelpPressed += () => SelectDestination(LauncherDestination.Help);
        Actions.HomeDestinationPressed += SelectDestination;
        var diagnostics = BuildLogColumn(profile, Actions.HelpDiagnosticsHost, dismissKeyboard);
        Log = diagnostics.Log;
        DiagnosticsDrawer = diagnostics.Drawer;
        DiagnosticsToggle = diagnostics.Toggle;
        Actions.HelpDiagnosticsHost.AddChild(
            BuildHelpAttributionFooter(_profile.Scale, _profile.Compact)
        );
        var navigation = BuildDestinationNavigation(profile);
        shell.Content.AddChild(navigation.Root);
        if (!profile.Compact)
            shell.Content.MoveChild(navigation.Root, 1);
        _destinationNavigationFrame = navigation.Root;
        _destinationSafeAreaSpacer = profile.Compact && OperatingSystem.IsAndroid()
            ? new Control { MouseFilter = Control.MouseFilterEnum.Ignore }
            : null;
        if (_destinationSafeAreaSpacer is not null)
            shell.Content.AddChild(_destinationSafeAreaSpacer);
        _destinationButtons = navigation.Buttons;
        UpdateSystemInsets();
        WireDestinationNavigation();
        SelectDestination(LauncherDestination.Home);
        _compactCurrentTaskTarget = FirstRunGuide;
        _compactScrollAnchorTarget = FirstRunGuide;
        WireCompactCurrentTaskNavigation();
        WireCompactWorkflowStepNavigation();
        WireCompactStatusDetailToggle();
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("Start here", FirstRunGuide, "Setup guide");
    }
}
