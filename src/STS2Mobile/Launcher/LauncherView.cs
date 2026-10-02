using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;
using System.Collections.Generic;

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
        Actions.HelpDiagnosticsHost.AddChild(BuildHelpAttributionFooter(_profile.Scale, _profile.Compact));
        var navigation = BuildDestinationNavigation(profile);
        shell.Content.AddChild(navigation.Root);
        if (!profile.Compact)
            shell.Content.MoveChild(navigation.Root, 1);
        _destinationNavigationFrame = navigation.Root;
        _destinationSafeAreaSpacer = profile.Compact && OperatingSystem.IsAndroid() ? new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        }

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

    internal void WireEvents(Action<string, string> loginRequested, Action<string> codeSubmitted, Action downloadRequested, Action<string> gameBranchChanged, Action<string> rendererModeChanged, Action launchPressed, Action retryPressed, Action checkForUpdatesPressed, Action updateSelectedVersionPressed, Action refreshGameVersionsPressed, Action redownloadPressed, Action diagnosticsPressed, Action showLastErrorPressed, Action copyRawLogPressed, Action safeLaunchPressed, Action saveSyncNowPressed, Action savePullPressed, Action savePushPressed, Action workshopSyncPressed, Action workshopClearPressed, Action modsSelectionChanged = null, Action reportBugPressed = null, Action checkAppUpdatesPressed = null)
    {
        Login.LoginRequested += loginRequested;
        Login.StatusRequested += SetStatus;
        Code.CodeSubmitted += codeSubmitted;
        Download.DownloadRequested += downloadRequested;
        Download.GameBranchChanged += gameBranchChanged;
        Download.RefreshGameVersionsRequested += refreshGameVersionsPressed;
        Actions.GameBranchChanged += gameBranchChanged;
        Actions.RendererModeChanged += rendererModeChanged;
        Actions.LaunchPressed += launchPressed;
        Actions.RetryPressed += retryPressed;
        Actions.CheckForUpdatesPressed += checkForUpdatesPressed;
        Actions.UpdateSelectedVersionPressed += updateSelectedVersionPressed;
        Actions.RefreshGameVersionsPressed += refreshGameVersionsPressed;
        Actions.RedownloadPressed += redownloadPressed;
        Actions.DiagnosticsPressed += diagnosticsPressed;
        Actions.ShowLastErrorPressed += showLastErrorPressed;
        Actions.CopyRawLogPressed += copyRawLogPressed;
        if (reportBugPressed != null)
            Actions.ReportBugPressed += reportBugPressed;
        if (checkAppUpdatesPressed != null)
            Actions.CheckAppUpdatesPressed += checkAppUpdatesPressed;
        Actions.SafeLaunchPressed += safeLaunchPressed;
        Actions.SaveSyncNowPressed += saveSyncNowPressed;
        Actions.SavePullPressed += savePullPressed;
        Actions.SavePushPressed += savePushPressed;
        Actions.WorkshopSyncPressed += workshopSyncPressed;
        Actions.WorkshopClearPressed += workshopClearPressed;
        if (modsSelectionChanged != null)
            Actions.ModsSelectionChanged += modsSelectionChanged;
    }

    internal void HideActions()
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("Start here", FirstRunGuide, "Setup guide");
        Actions.HideAll();
        ScrollCompactPrimaryTo(FirstRunGuide);
    }

    internal void ShowRetry()
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactWorkflowStep(CompactWorkflowStep.Play);
        SetCompactCurrentTask("Retry", Actions.RetryScrollTarget, "Restart safely");
        Actions.ShowRetry();
        ScrollCompactPrimaryTo(Actions.RetryScrollTarget);
    }

    internal void ShowHomeHelpAction() => Actions.ShowHomeHelpAction();
    internal void ShowLaunchActions(string launchText, bool showUpdate)
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(false);
        SetCompactWorkflowStep(CompactWorkflowStep.Play);
        Actions.ShowLaunch(launchText, showUpdate);
        SetCompactCurrentTask("Play", Actions.ReadyScrollTarget, "Ready");
        ScrollCompactPrimaryTo(Actions.ReadyScrollTarget);
    }

    private void SetCompactReadyInstallSectionVisible(bool visible)
    {
        if (!_profile.Compact)
            return;
        Download.Visible = visible;
    }

    internal void SetActionPreferences(LauncherPreferences.ActionPreferences preferences)
    {
        Actions.SetRendererMode(preferences.RendererMode);
        SetGameBranch(preferences.GameBranch);
    }

    internal void SetActionPreferences(LauncherPreferences.ActionPreferences preferences, System.Collections.Generic.IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        Actions.SetRendererMode(preferences.RendererMode);
        Download.SetGameBranchOptions(preferences.GameBranch, branches);
        Actions.SetGameBranchOptions(preferences.GameBranch, branches);
    }

    internal void SetWorkshopButtonsDisabled(bool disabled) => Actions.SetWorkshopButtonsDisabled(disabled);
    internal void SetLaunchControlsDisabled(bool disabled) => Actions.SetLaunchControlsDisabled(disabled);
    internal void SetSaveSyncControlsDisabled(bool disabled) => Actions.SetSaveSyncControlsDisabled(disabled);
    internal void SetSaveSyncPresentation(LauncherSaveSyncPresentation presentation) => Actions.SetSaveSyncPresentation(presentation);
    internal void SetModsPresentation(LauncherModsPresentation presentation) => Actions.SetModsPresentation(presentation);
    internal void SetPowerVrCompatibility(bool required) => Actions.SetPowerVrCompatibility(required);
    internal void SetRendererMode(string mode) => Actions.SetRendererMode(mode);
    internal void SetUpdateCheckBusy(bool busy)
    {
        Actions.SetUpdateButtonDisabled(busy);
        Actions.SetVersionSelectionDisabled(busy);
        if (busy)
        {
            Actions.SetVersionCheckOutcome(LauncherVersionCheckOutcome.None);
            Actions.SetUpdateButtonText("Checking...");
        }
    }

    internal void SetVersionPrimaryAction(LauncherVersionPrimaryAction action) => Actions.SetVersionPrimaryAction(action);
    internal void SetVersionCheckOutcome(LauncherVersionCheckOutcome outcome) => Actions.SetVersionCheckOutcome(outcome);
    internal void SetUpdateButtonText(string text) => Actions.SetUpdateButtonText(text);
    internal void ClearLoginPasswordAndDisable()
    {
        Login.SetDisabled(true);
        Login.ClearPassword();
    }

    internal void SetLoginFormVisible(bool visible, bool disabled)
    {
        if (visible)
        {
            SelectHomeDestination();
            SetFirstRunGuideVisible(false);
            HideCompactCompletedAuthSections(showCode: false);
        }

        Login.SetFormVisible(visible, disabled);
        if (visible)
        {
            SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
            SetCompactCurrentTask("Sign in", Login, "Steam account");
            ScrollCompactPrimaryTo(Login);
        }
    }

    internal void ShowCodePrompt(bool wasIncorrect)
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: true);
        SetCompactWorkflowStep(CompactWorkflowStep.Code);
        SetCompactCurrentTask("Verify", Code, "Steam Guard code");
        Code.Show(wasIncorrect);
        ScrollCompactPrimaryTo(Code);
    }

    private void SetFirstRunGuideVisible(bool visible) => FirstRunGuide.Visible = visible;
    private void HideCompactCompletedAuthSections(bool showCode)
    {
        if (!_profile.Compact)
            return;
        Login.SetFormVisible(false, disabled: true);
        Code.Visible = showCode;
    }

    internal void HideAllSections()
    {
        Login.Visible = false;
        Code.Visible = false;
        Download.Visible = false;
        Actions.HideAll();
    }

    internal void ShowDownloadAction(string buttonText)
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.Visible = true;
        Download.Reset(buttonText);
        ScrollCompactPrimaryTo(Download);
    }

    internal void SetGameBranch(string branch)
    {
        Download.SetGameBranch(branch);
        Actions.SetGameBranch(branch);
    }

    internal void SetGameBranchOptions(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        Download.SetAvailableBranches(branches);
        Actions.SetAvailableBranches(branches);
    }

    internal void ShowDownloadProgress(string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.ShowProgress(text);
    }

    internal void SetDownloadProgress(double percentage, string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        Download.SetProgress(percentage, text);
    }

    internal void HideDownload() => Download.Visible = false;
    internal void ResetDownload() => Download.Reset();
    internal void ResetDownload(string buttonText) => Download.Reset(buttonText);
    internal void SetDownloadButtonDisabled(bool disabled) => Download.SetButtonDisabled(disabled);
    internal void SetRefreshGameVersionsBusy(bool busy)
    {
        Download.SetRefreshVersionsButtonDisabled(busy);
        Actions.SetRefreshVersionsButtonDisabled(busy);
    }

    private void ScrollCompactPrimaryTo(Control target)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(target))
            return;
        _compactScrollAnchorTarget = target;
        PrimaryScroll.ScrollVertical = 0;
    }
}
