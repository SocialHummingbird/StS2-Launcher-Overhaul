using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection : VBoxContainer
{
    private const string PushButtonText = "Push Saves to Steam Cloud";
    private const string PushConfirmButtonText = "Confirm: Overwrite Steam Cloud";
    private const int CompactReadySummaryBranchLimit = 14;
    private const int CompactReadyStackedSummaryBranchLimit = 28;
    private const int CompactReadyVersionHelpBranchLimit = 22;
    private const int CompactReadyVersionHelpStackedBranchLimit = 30;
    private const int CompactReadyVersionHelpHeight = 54;
    private const int CompactReadyVersionHelpFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactCloudPrimaryActionSeparation = 6;
    private const int CompactCloudOptionToggleSeparation = 6;
    private const int CompactCloudSafetyDetailHeight = 50;
    private const int CompactCloudSafetyDetailFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactCloudPushWarningHeight = 50;
    private const int CompactCloudPushWarningFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;

    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<string> GameBranchChanged;
    internal event Action<bool> LocalBackupToggled;
    internal event Action<bool> CloudSyncToggled;
    internal event Func<bool> CloudPushArmRequested;
    internal event Action CloudPushPressed;
    internal event Action CloudPullPressed;
    internal event Action CheckForUpdatesPressed;
    internal event Action RefreshGameVersionsPressed;
    internal event Action RedownloadPressed;
    internal event Action ClearCachedVersionsPressed;
    internal event Action DiagnosticsPressed;
    internal event Action ShowLastErrorPressed;
    internal event Action CopyRawLogPressed;
    internal event Action SafeLaunchPressed;
    internal event Action WorkshopSyncPressed;
    internal event Action WorkshopClearPressed;

    private readonly Button _launchButton;
    private readonly Button _safeLaunchButton;
    private readonly Button _retryButton;
    private readonly float _scale;
    private readonly bool _compact;
    private readonly bool _compactStackedActionRows;
    private readonly OptionButton _branchDropdown;
    private readonly Label _branchHelpLabel;
    private readonly Button _branchDetailsToggle;
    private readonly Button _readyVersionSummaryPanel;
    private readonly Label _readyVersionSummaryLabel;
    private readonly Label _cloudSafetyLabel;
    private readonly Button _cloudSafetyToggle;
    private readonly Button _cloudOptionsToggle;
    private readonly Container _compactCloudOptionsRow;
    private readonly Button _localBackupToggle;
    private readonly Button _cloudSyncToggle;
    private readonly Button _pushButton;
    private readonly Button _cloudPushToggle;
    private readonly Button _confirmPushButton;
    private readonly Label _pushConfirmationLabel;
    private readonly Button _pullButton;
    private readonly Button _updateButton;
    private readonly Button _refreshVersionsButton;
    private readonly Button _redownloadButton;
    private readonly Button _clearCachedVersionsButton;
    private readonly Button _workshopSyncButton;
    private readonly Button _workshopClearButton;
    private readonly Button _diagnosticsButton;
    private readonly Button _showLastErrorButton;
    private readonly Button _copyRawLogButton;
    private readonly VBoxContainer _cloudGroup;
    private readonly VBoxContainer _supportGroup;
    private readonly GridContainer _supportToolsGrid;
    private readonly VBoxContainer _pushPullRow;
    private readonly Button _supportToggle;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private bool _supportExpanded;
    private bool _branchDetailsExpanded;
    private bool _branchControlsAvailable;
    private bool _cloudSafetyExpanded;
    private bool _cloudOptionsExpanded;
    private bool _cloudPushExpanded;
    private bool _localBackupEnabled;
    private bool _cloudSyncEnabled;
    private string _gameBranch = SteamGameBranch.Public;

    internal void SetLocalBackupChecked(bool value)
        => ApplyLocalBackupToggle(value);

    internal void SetCloudSyncChecked(bool value)
        => ApplyCloudSyncToggle(value);

    internal void SetUpdateButtonText(string text) => SetCompactActionButtonText(_updateButton, text);

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;

    internal void SetRefreshVersionsButtonDisabled(bool disabled) => _refreshVersionsButton.Disabled = disabled;

    internal void SetWorkshopButtonsDisabled(bool disabled)
    {
        _workshopSyncButton.Disabled = disabled;
        _workshopClearButton.Disabled = disabled;
    }
}
