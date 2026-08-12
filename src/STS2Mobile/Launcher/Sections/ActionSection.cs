using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection : VBoxContainer
{
    private const int CompactReadySummaryBranchLimit = 14;
    private const int CompactReadyStackedSummaryBranchLimit = 28;
    private const int CompactReadyVersionHelpBranchLimit = 22;
    private const int CompactReadyVersionHelpStackedBranchLimit = 30;
    private const int CompactReadyVersionHelpHeight = 54;
    private const int CompactReadyVersionHelpFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactActionSeparation = 6;

    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<string> GameBranchChanged;
    internal event Action<string> RendererModeChanged;
    internal event Action CheckForUpdatesPressed;
    internal event Action RefreshGameVersionsPressed;
    internal event Action RedownloadPressed;
    internal event Action ClearCachedVersionsPressed;
    internal event Action DiagnosticsPressed;
    internal event Action ShowLastErrorPressed;
    internal event Action CopyRawLogPressed;
    internal event Action SafeLaunchPressed;
    internal event Action SaveSyncNowPressed;
    internal event Action SavePullPressed;
    internal event Action SavePushPressed;
    internal event Action WorkshopSyncPressed;
    internal event Action WorkshopClearPressed;
    internal event Action ModsSelectionChanged;

    private readonly Button _launchButton;
    private readonly Button _safeLaunchButton;
    private readonly VBoxContainer _homeJourney;
    private readonly Label _homeAccountState;
    private readonly Label _homeGameState;
    private readonly Label _homeSaveState;
    private readonly Label _homeSaveNamespaceState;
    private readonly VBoxContainer _rendererGroup;
    private readonly Button _rendererAutoButton;
    private readonly Button _rendererVulkanButton;
    private readonly Button _rendererOpenGlButton;
    private readonly Button _retryButton;
    private readonly float _scale;
    private readonly bool _compact;
    private readonly bool _compactStackedActionRows;
    private readonly OptionButton _branchDropdown;
    private readonly Label _branchHelpLabel;
    private readonly Button _branchDetailsToggle;
    private readonly Button _readyVersionSummaryPanel;
    private readonly Label _readyVersionSummaryLabel;
    private readonly Button _updateButton;
    private readonly Button _refreshVersionsButton;
    private readonly Button _redownloadButton;
    private readonly Button _clearCachedVersionsButton;
    private readonly Button _workshopSyncButton;
    private readonly Button _workshopClearButton;
    private readonly VBoxContainer _saveSyncGroup;
    private readonly Button _saveSyncNowButton;
    private readonly Button _savePullButton;
    private readonly Button _savePushButton;
    private readonly Label _saveSyncStatus;
    private readonly Label _saveLastSuccessState;
    private readonly Label _saveLocalState;
    private readonly Label _saveSteamState;
    private readonly VBoxContainer _modsGroup;
    private readonly Button _playVanillaButton;
    private readonly Button _playModdedButton;
    private readonly Label _modsSelectedModeLabel;
    private readonly Label _modsSaveNamespaceLabel;
    private readonly Label _modsStatusLabel;
    private readonly VBoxContainer _modsList;
    private readonly List<Button> _modToggleButtons = new();
    private readonly List<string> _modToggleKeys = new();
    private readonly List<bool> _modToggleCanChange = new();
    private readonly Button _diagnosticsButton;
    private readonly Button _showLastErrorButton;
    private readonly Button _copyRawLogButton;
    private readonly VBoxContainer _supportGroup;
    private readonly Button _supportToggle;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;
    private VBoxContainer _homeDestination;
    private VBoxContainer _savesDestination;
    private VBoxContainer _versionsDestination;
    private VBoxContainer _modsDestination;
    private VBoxContainer _helpDestination;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private bool _supportExpanded;
    private bool _branchDetailsExpanded;
    private bool _branchControlsAvailable;
    private bool _launchControlsDisabled;
    private bool _workshopButtonsDisabled;
    private bool _powerVrCompatibilityRequired;
    private LauncherDestination _destination;
    private int _readySummaryEnabledModCount;
    private string _contextualPlayLabel = "Play Vanilla";
    private string _gameBranch = SteamGameBranch.Public;
    private string _rendererMode = LauncherRendererMode.Auto;

    internal void SetUpdateButtonText(string text) => SetCompactActionButtonText(_updateButton, text);

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
        for (var i = 0; i < _modToggleButtons.Count; i++)
        {
            _modToggleButtons[i].Disabled =
                ContextControlsDisabled || !_modToggleCanChange[i];
        }
    }
}
