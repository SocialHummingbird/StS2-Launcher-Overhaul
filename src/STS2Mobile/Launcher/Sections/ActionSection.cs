using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

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
    private LauncherVersionPrimaryAction _versionPrimaryAction =
        LauncherVersionPrimaryAction.CheckForUpdates;
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
        SetCompactActionButtonText(
            _updateButton,
            action == LauncherVersionPrimaryAction.UpdateSelectedVersion
                ? "Update selected Steam branch"
                : "Check for updates"
        );
        _updateButton.AccessibilityName =
            action == LauncherVersionPrimaryAction.UpdateSelectedVersion
                ? "Update selected Steam branch"
                : "Check for updates";
    }

    internal void SetVersionSelectionDisabled(bool disabled)
        => _branchDropdown.Disabled = disabled;

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

    private bool ContextControlsDisabled
        => _launchControlsDisabled;

    private void ApplyContextControlsDisabled()
    {
        _playVanillaButton.Disabled = ContextControlsDisabled;
        _playModdedButton.Disabled = ContextControlsDisabled;
        _workshopSyncButton.Disabled =
            _workshopButtonsDisabled || ContextControlsDisabled;
        _workshopClearButton.Disabled =
            _workshopButtonsDisabled || ContextControlsDisabled;
        for (var i = 0; i < _modRows.Count; i++)
        {
            _modRows[i].EnabledToggle.Disabled =
                ContextControlsDisabled || !_modRows[i].CanChange;
        }
    }
}
