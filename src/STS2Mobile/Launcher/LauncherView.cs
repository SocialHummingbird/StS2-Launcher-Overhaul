using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

// Owns launcher view references and UI behavior.
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
    private readonly ColorRect _statusAccent;
    private readonly StyledLabel[] _workflowStepNumberLabels;
    private readonly StyledLabel[] _workflowStepLabels;
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
    private static readonly string[] CompactWorkflowStepNames =
    {
        "SIGN IN",
        "GUARD",
        "FILES",
        "PLAY",
    };
    private static readonly string[] CompactWorkflowStepNumbers =
    {
        "1",
        "2",
        "3",
        "4",
    };
    private static readonly string[] CompactWorkflowStepTooltips =
    {
        "Sign in",
        "Steam Guard",
        "Files",
        "Play",
    };

    private enum CompactWorkflowStep
    {
        SignIn = 0,
        Code = 1,
        Files = 2,
        Play = 3,
    }

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
        _statusAccent = primary.StatusAccent;
        _workflowStepNumberLabels = primary.WorkflowStepNumberLabels;
        _workflowStepLabels = primary.WorkflowStepLabels;
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
        _compactCurrentTaskButton.Pressed += () => ScrollCompactPrimaryTo(_compactCurrentTaskTarget);
        WireCompactWorkflowStepNavigation();
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("GO TO SETUP", FirstRunGuide, "Setup guide");
    }

    internal void SetStatus(string text)
    {
        var phase = LauncherPortalStatusFormatter.PhaseFor(text);
        var color = LauncherPortalStatusFormatter.ColorFor(phase);
        _statusPhaseLabel.Text = phase;
        _statusPhaseLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        _statusActionLabel.Text = LauncherPortalStatusFormatter.ActionFor(text);
        _statusAccent.Color = color;
        var message = LauncherPortalStatusFormatter.MessageFor(text);
        _statusLabel.Text = message;
        _statusLabel.TooltipText = message;
        if (_profile.Compact)
        {
            var attention = string.Equals(phase, "ATTENTION", StringComparison.Ordinal);
            _statusLabel.AutowrapMode = attention
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off;
            _statusLabel.ClipText = !attention;
        }
    }

    internal void AppendLog(string msg) => AppendLogLine(Log, msg);

    internal void AppendColoredLog(string msg, Godot.Color color)
        => AppendColoredLogLine(Log, msg, color);

    internal void ShowDiagnosticsConsole()
    {
        DiagnosticsDrawer.Visible = true;
        SetDiagnosticsToggleText(DiagnosticsToggle, _profile, visible: true);
        if (_profile.Compact)
            ScrollCompactPrimaryTo(DiagnosticsDrawer);
    }

    internal void HideAllSections()
    {
        Login.Visible = false;
        Code.Visible = false;
        Download.Visible = false;
        Actions.HideAll();
    }

    private void SetCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact || _workflowStepLabels.Length == 0)
            return;

        var activeIndex = (int)step;
        for (var i = 0; i < _workflowStepLabels.Length; i++)
        {
            var active = i == activeIndex;
            var complete = i < activeIndex;
            var color = active
                ? LauncherComponentTheme.OrangeHot
                : complete
                    ? LauncherComponentTheme.CyanAccent
                    : LauncherComponentTheme.TextMuted;
            _workflowStepLabels[i].AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                color
            );
            if (i < _workflowStepNumberLabels.Length)
            {
                _workflowStepNumberLabels[i].AddThemeColorOverride(
                    LauncherViewLayoutMetrics.ThemeFontColor,
                    color
                );
            }
            _workflowStepAccents[i].Color = active
                ? LauncherComponentTheme.OrangeAccent
                : complete
                    ? LauncherComponentTheme.CyanDim
                    : LauncherComponentTheme.ButtonNormal;
        }
    }

    private void WireCompactWorkflowStepNavigation()
    {
        if (!_profile.Compact)
            return;

        for (var i = 0; i < _workflowStepButtons.Length; i++)
        {
            var capturedStep = (CompactWorkflowStep)i;
            _workflowStepButtons[i].Pressed += () => ScrollCompactWorkflowStep(capturedStep);
        }
    }

    private void ScrollCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact)
            return;

        var target = step switch
        {
            CompactWorkflowStep.SignIn => Login.Visible
                ? Login
                : (FirstRunGuide.Visible ? FirstRunGuide : _compactCurrentTaskTarget),
            CompactWorkflowStep.Code => Code.Visible ? Code : _compactCurrentTaskTarget,
            CompactWorkflowStep.Files => Download.Visible ? Download : _compactCurrentTaskTarget,
            CompactWorkflowStep.Play => _compactCurrentTaskTarget,
            _ => _compactCurrentTaskTarget,
        };
        ScrollCompactPrimaryTo(target);
    }

    private void SetCompactCurrentTask(string text, Control target, string detail)
    {
        if (!_profile.Compact)
            return;

        SetCompactCurrentTaskButtonText(_compactCurrentTaskButton, _scale, text, detail);
        _compactCurrentTaskButton.Visible = true;
        _compactCurrentTaskTarget = target;
        _compactScrollAnchorTarget = target;
    }
}
