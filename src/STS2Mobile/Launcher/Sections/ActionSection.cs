using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class ActionSection : VBoxContainer
{
    private const int CompactActionSeparation = 6;
    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<string> GameBranchChanged;
    internal event Action<string> RendererModeChanged;
    internal event Action CheckForUpdatesPressed;
    internal event Action UpdateSelectedVersionPressed;
    internal event Action RefreshGameVersionsPressed;
    internal event Action RedownloadPressed;
    internal event Action DiagnosticsPressed;
    internal event Action ShowLastErrorPressed;
    internal event Action CopyRawLogPressed;
    internal event Action ReportBugPressed;
    internal event Action CheckAppUpdatesPressed;
    internal event Action SafeLaunchPressed;
    internal event Action SaveSyncNowPressed;
    internal event Action SavePullPressed;
    internal event Action SavePushPressed;
    internal event Action WorkshopSyncPressed;
    internal event Action WorkshopClearPressed;
    internal event Action ModsSelectionChanged;
    internal event Action HomeHelpPressed;
    private readonly Button _launchButton;
    private readonly Button _safeLaunchButton;
    private readonly VBoxContainer _homeJourney;
    private readonly Label _homeStateLine;
    private readonly Button _homeHelpButton;
    private readonly VBoxContainer _rendererGroup;
    private readonly Button _rendererAutoButton;
    private readonly Button _rendererVulkanButton;
    private readonly Button _rendererOpenGlButton;
    private readonly Button _retryButton;
    private readonly float _scale;
    private readonly bool _compact;
    private readonly bool _compactStackedActionRows;
    private readonly VBoxContainer _versionSelectionGroup;
    private readonly OptionButton _branchDropdown;
    private readonly Label _branchHelpLabel;
    private readonly Button _updateButton;
    private readonly Button _refreshVersionsButton;
    private readonly Button _redownloadButton;
    private readonly Button _workshopSyncButton;
    private readonly Button _workshopClearButton;
    private readonly VBoxContainer _saveSyncGroup;
    private readonly Button _saveSyncNowButton;
    private readonly Button _savePullButton;
    private readonly Button _savePushButton;
    private readonly Label _saveSyncStatus;
    private readonly GridContainer _saveSyncDetails;
    private readonly Label _saveLocalState;
    private readonly Label _saveSteamState;
    private readonly VBoxContainer _modsGroup;
    private readonly Button _playVanillaButton;
    private readonly Button _playModdedButton;
    private readonly Label _modsLaunchSummaryLabel;
    private readonly VBoxContainer _modsList;
    private readonly List<ModRowControls> _modRows = new();
    private readonly Button _diagnosticsButton;
    private readonly Button _showLastErrorButton;
    private readonly Button _copyRawLogButton;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;
    private VBoxContainer _homeDestination;
    private VBoxContainer _savesDestination;
    private VBoxContainer _versionsDestination;
    private VBoxContainer _modsDestination;
    private VBoxContainer _helpDestination;
    private VBoxContainer _versionMaintenanceGroup;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private bool _launchControlsDisabled;
    private bool _workshopButtonsDisabled;
    private bool _powerVrCompatibilityRequired;
    private LauncherDestination _destination;
    private string _contextualPlayLabel = "Play Vanilla";
    private string _homeSaveNamespace = "Vanilla saves";
    private string _homeSyncState = "Not synced yet";
    private string _gameBranch = SteamGameBranch.Public;
    private bool _selectedVersionInstalled;
    private LauncherVersionPrimaryAction _versionPrimaryAction = LauncherVersionPrimaryAction.CheckForUpdates;
    private LauncherVersionCheckOutcome _versionCheckOutcome;
    private string _rendererMode = LauncherRendererMode.Auto;
    private void UpdateVersionActionAvailability()
    {
        _redownloadButton.Visible = _selectedVersionInstalled;
        if (_versionMaintenanceGroup != null)
            _versionMaintenanceGroup.Visible = _redownloadButton.Visible;
    }

    internal void SetUpdateButtonText(string text) => SetCompactActionButtonText(_updateButton, text);
    internal void SetVersionPrimaryAction(LauncherVersionPrimaryAction action)
    {
        _versionPrimaryAction = action;
        SetCompactActionButtonText(_updateButton, action == LauncherVersionPrimaryAction.UpdateSelectedVersion ? "Update selected Steam branch" : "Check for updates");
        _updateButton.AccessibilityName = action == LauncherVersionPrimaryAction.UpdateSelectedVersion ? "Update selected Steam branch" : "Check for updates";
    }

    internal void SetVersionSelectionDisabled(bool disabled) => _branchDropdown.Disabled = disabled;
    internal void SetVersionCheckOutcome(LauncherVersionCheckOutcome outcome)
    {
        _versionCheckOutcome = outcome;
        UpdateBranchHelpText();
    }

    private void InvokeVersionPrimaryAction()
    {
        if (_versionPrimaryAction == LauncherVersionPrimaryAction.UpdateSelectedVersion)
            UpdateSelectedVersionPressed?.Invoke();
        else
            CheckForUpdatesPressed?.Invoke();
    }

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;
    internal void SetRefreshVersionsButtonDisabled(bool disabled) => _refreshVersionsButton.Disabled = disabled;
    internal void SetWorkshopButtonsDisabled(bool disabled)
    {
        _workshopButtonsDisabled = disabled;
        ApplyContextControlsDisabled();
        if (!ContextControlsDisabled && !disabled && _modsGroup.Visible)
            RefreshModsStatus();
    }

    internal void SetLaunchControlsDisabled(bool disabled)
    {
        _launchControlsDisabled = disabled;
        ApplyLaunchControlsDisabled();
        ApplyContextControlsDisabled();
    }

    internal void SetPowerVrCompatibility(bool required)
    {
        _powerVrCompatibilityRequired = required;
        const string compatibilityReason = "OpenGL is required on this PowerVR device for working touch input.";
        _rendererAutoButton.TooltipText = required ? compatibilityReason : "Use the project's default renderer.";
        _rendererVulkanButton.TooltipText = required ? compatibilityReason : "Use the Vulkan Mobile renderer.";
        _rendererOpenGlButton.TooltipText = required ? compatibilityReason : "Use the OpenGL Compatibility renderer.";
        _rendererAutoButton.AccessibilityDescription = _rendererAutoButton.TooltipText;
        _rendererVulkanButton.AccessibilityDescription = _rendererVulkanButton.TooltipText;
        _rendererOpenGlButton.AccessibilityDescription = _rendererOpenGlButton.TooltipText;
        ApplyLaunchControlsDisabled();
    }

    internal VBoxContainer HelpDiagnosticsHost => _helpDestination;

    private void ApplyLaunchControlsDisabled()
    {
        var disabled = _launchControlsDisabled;
        _launchButton.Disabled = disabled;
        _safeLaunchButton.Disabled = disabled;
        _rendererAutoButton.Disabled = disabled || _powerVrCompatibilityRequired;
        _rendererVulkanButton.Disabled = disabled || _powerVrCompatibilityRequired;
        _rendererOpenGlButton.Disabled = disabled;
    }

    private bool ContextControlsDisabled => _launchControlsDisabled;

    private void ApplyContextControlsDisabled()
    {
        _playVanillaButton.Disabled = ContextControlsDisabled;
        _playModdedButton.Disabled = ContextControlsDisabled;
        _workshopSyncButton.Disabled = _workshopButtonsDisabled || ContextControlsDisabled;
        _workshopClearButton.Disabled = _workshopButtonsDisabled || ContextControlsDisabled;
        for (var i = 0; i < _modRows.Count; i++)
        {
            _modRows[i].EnabledToggle.Disabled = ContextControlsDisabled || !_modRows[i].CanChange;
        }
    }

    private Button AddPrimaryHiddenButton(Container parent, string text, float scale, Action pressed) => AddHiddenButton(parent, text, scale, LauncherSectionMetrics.PrimaryButtonFontSize, LauncherSectionMetrics.PrimaryButtonHeight, pressed);
    private Button AddSecondaryHiddenButton(Container parent, string text, float scale, Action pressed) => AddHiddenButton(parent, text, scale, LauncherSectionMetrics.SecondaryButtonFontSize, LauncherSectionMetrics.SecondaryButtonHeight, pressed);
    private Button AddHiddenButton(Container parent, string text, float scale, int fontSize, int height, Action pressed)
    {
        var button = new StyledButton(text, scale, fontSize: fontSize, height: height);
        button.Visible = false;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }

    private static Button AddActionButton(Container row, string text, float scale, Action pressed)
    {
        var button = new StyledButton(text, scale, LauncherSectionMetrics.SecondaryButtonFontSize, LauncherSectionMetrics.SecondaryButtonHeight);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        row.AddChild(button);
        return button;
    }

    private const string CompactActionButtonBodyName = "CompactActionButtonBody";
    private const string CompactActionButtonTitleName = "CompactActionButtonTitle";
    private const string CompactActionButtonDetailName = "CompactActionButtonDetail";
    private static readonly CompactButtonDetailLabelSpec CompactActionButtonLabels = CompactButtonDetailLabelSpec.Default(CompactActionButtonBodyName, CompactActionButtonTitleName, CompactActionButtonDetailName);
    private Button AddCompactSupportToolButton(Container parent, string text, float scale, Action pressed, string detail = null)
    {
        var button = AddHiddenButton(parent, string.IsNullOrEmpty(detail) ? text : CompactSupportToolText(text, detail), scale, LauncherSectionMetrics.CompactSupportToolFontSize, LauncherSectionMetrics.CompactSupportToolHeight, pressed);
        SetCompactActionButtonText(button, button.Text);
        return button;
    }

    private static string CompactSupportToolText(string text, string detail) => $"{text}\n{detail}";
    private void SetCompactActionButtonText(Button button, string text) => CompactButtonDetailLabels.Apply(button, text, _scale, _compact, CompactActionButtonLabels);
    internal ActionSection(float scale, bool compact = false, bool compactStackedActionRows = false)
    {
        _scale = scale;
        _compact = compact;
        _compactStackedActionRows = compactStackedActionRows;
        LauncherSectionSetup.ConfigureHiddenSection(this, scale, "Play", "Launch the game, choose a version, and manage mods.", LauncherComponentTheme.OrangeHot, compact, "Play safely");
        var toggleRadius = (int)(10 * scale);
        var toggleBorderWidth = Math.Max(1, (int)(2 * scale));
        _toggleOffStyle = LauncherStyleBoxes.MakeOutline(LauncherComponentTheme.ButtonHover, toggleRadius, toggleBorderWidth);
        _toggleOnStyle = LauncherStyleBoxes.MakeOutline(LauncherComponentTheme.OrangeAccent, toggleRadius, toggleBorderWidth);
        _toggleOffStyle.BgColor = LauncherComponentTheme.ButtonNormal;
        _toggleOnStyle.BgColor = LauncherComponentTheme.ButtonHover;
        var supportToolsParent = BuildActionGroup(scale);
        supportToolsParent.Visible = false;
        AddChild(supportToolsParent);
        var primaryActions = BuildPrimaryActionControls(scale, compact, supportToolsParent);
        _retryButton = primaryActions.RetryButton;
        _launchButton = primaryActions.LaunchButton;
        _safeLaunchButton = primaryActions.SafeLaunchButton;
        var rendererControls = BuildRendererControls(scale, compact);
        _rendererGroup = rendererControls.Group;
        _rendererAutoButton = rendererControls.AutoButton;
        _rendererVulkanButton = rendererControls.VulkanButton;
        _rendererOpenGlButton = rendererControls.OpenGlButton;
        ApplyRendererMode(_rendererMode, notify: false);
        var homeJourney = BuildHomeJourney(scale, compact);
        _homeJourney = homeJourney.Group;
        _homeStateLine = homeJourney.StateLine;
        _homeHelpButton = homeJourney.HelpButton;
        var branchControls = BuildBranchControls(scale, compact);
        _versionSelectionGroup = branchControls.Group;
        _branchDropdown = branchControls.Dropdown;
        _branchHelpLabel = branchControls.StateLabel;
        var saveSyncControls = BuildSaveSyncControls(scale, compact);
        _saveSyncGroup = saveSyncControls.Group;
        _saveSyncNowButton = saveSyncControls.SyncNowButton;
        _savePullButton = saveSyncControls.PullButton;
        _savePushButton = saveSyncControls.PushButton;
        _saveSyncStatus = saveSyncControls.Status;
        _saveSyncDetails = saveSyncControls.Details;
        _saveLocalState = saveSyncControls.LocalState;
        _saveSteamState = saveSyncControls.SteamState;
        var modsControls = BuildModsControls(scale, compact);
        _modsGroup = modsControls.Group;
        _playVanillaButton = modsControls.PlayVanillaButton;
        _playModdedButton = modsControls.PlayModdedButton;
        _modsLaunchSummaryLabel = modsControls.LaunchSummaryLabel;
        _modsList = modsControls.ModsList;
        _workshopSyncButton = modsControls.WorkshopSyncButton;
        _workshopClearButton = modsControls.WorkshopClearButton;
        var supportControls = BuildSupportControls(scale, compact, supportToolsParent);
        _updateButton = supportControls.UpdateButton;
        _refreshVersionsButton = supportControls.RefreshVersionsButton;
        _redownloadButton = supportControls.RedownloadButton;
        _diagnosticsButton = supportControls.DiagnosticsButton;
        _showLastErrorButton = supportControls.ShowLastErrorButton;
        _copyRawLogButton = supportControls.CopyRawLogButton;
        SetGameBranch(_gameBranch);
        BuildDestinationLayout();
        supportToolsParent.QueueFree();
    }

    private (Button RetryButton, Button LaunchButton, Button SafeLaunchButton) BuildPrimaryActionControls(float scale, bool compact, Container supportToolsParent)
    {
        var retryButton = AddHiddenButton(this, "Try Again", scale, LauncherSectionMetrics.PrimaryButtonFontSize, LauncherSectionMetrics.PrimaryButtonHeight, () => RetryPressed?.Invoke());
        LauncherButtonStyles.ApplyPrimaryAction(retryButton, scale);
        SetCompactActionButtonText(retryButton, retryButton.Text);
        var launchButton = AddPrimaryHiddenButton(this, "Play", scale, () => LaunchPressed?.Invoke());
        launchButton.Name = "Play";
        launchButton.AccessibilityName = "Play";
        LauncherButtonStyles.ApplyPrimaryAction(launchButton, scale);
        var safeLaunchButton = compact ? AddCompactSupportToolButton(supportToolsParent, "Safe Start", scale, () => SafeLaunchPressed?.Invoke()) : AddSecondaryHiddenButton(this, "Safe Start", scale, () => SafeLaunchPressed?.Invoke());
        LauncherButtonStyles.ApplyPrimaryAction(safeLaunchButton, scale);
        safeLaunchButton.AccessibilityName = "Safe Start";
        safeLaunchButton.AccessibilityDescription = "Uses local saves and skips shader warmup for one run. Uses OpenGL on PowerVR; otherwise uses the game's default renderer.";
        return (retryButton, launchButton, safeLaunchButton);
    }

    private static VBoxContainer BuildActionGroup(float scale)
    {
        var group = new VBoxContainer();
        group.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        group.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale));
        return group;
    }

    private static Container BuildCompactActionRow(Container parent, float scale, bool compactStackedActionRows)
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactActionSeparation, scale));
        parent.AddChild(row);
        return row;
    }
}
