using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher.Components;
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

    internal void SetGameBranch(string branch)
    {
        var normalizedBranch = SteamGameBranch.Normalize(branch);
        var branchChanged = !string.Equals(_gameBranch, normalizedBranch, StringComparison.OrdinalIgnoreCase);
        _gameBranch = normalizedBranch;
        PopulateBranchDropdown();
        if (branchChanged)
        {
            CollapseCompactBranchDetailsAfterSelection();
            return;
        }

        UpdateBranchHelpText();
    }

    internal void SetAvailableBranches(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _availableBranches = branches ?? Array.Empty<LauncherBranchCatalog.BranchOption>();
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    private void UpdateBranchHelpText()
    {
        _branchHelpLabel.Text = _compact
            ? CompactReadyVersionHelpText()
            : SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
                + "\n"
                + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches)
                + "\n"
                + "Version/download actions affect local game files only. Steam Cloud saves move only through Pull/Push.";
        _branchHelpLabel.Visible = _branchDropdown.Visible && (!_compact || _branchDetailsExpanded);
        if (_branchDetailsToggle != null)
        {
            SetCompactActionButtonText(_branchDetailsToggle, _compact
                ? (_branchDetailsExpanded
                    ? CompactPlaySyncDrawerText("Hide Version", "Keep active")
                    : CompactPlaySyncDrawerText(
                        $"Change Version: {SteamGameBranch.CompactDisplayName(_gameBranch, 14)}",
                        "Version target"
                    ))
                : (_branchDetailsExpanded
                    ? "Hide Version Details"
                    : "Show Version Details"));
        }
        if (_cloudSafetyLabel != null)
        {
            _cloudSafetyLabel.Text = _compact
                ? CompactCloudSafetyDetailText()
                : $"Steam Cloud actions apply to selected version: {SteamGameBranch.DisplayName(_gameBranch)}.\nPull copies Steam Cloud saves to Android. Push copies Android saves to Steam Cloud and can overwrite remote saves.";
            _cloudSafetyLabel.Visible = !_compact || _cloudSafetyExpanded;
        }
        if (_readyVersionSummaryLabel != null)
        {
            _readyVersionSummaryLabel.Text = _compact
                ? CompactReadyVersionSummary()
                : $"Ready version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n"
                    + $"Slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}. Start Game and Pull/Push use this version.\n"
                    + "Cloud: Pull first. Push stays locked until explicitly opened.";
        }
        if (_cloudSafetyToggle != null)
        {
            _cloudSafetyToggle.Visible = _compact;
            SetCompactActionButtonText(_cloudSafetyToggle, _cloudSafetyExpanded
                ? CompactPlaySyncDrawerText("Hide Save Check", "Keep saves safe")
                : CompactCloudSafetySummary());
        }
    }

    internal void ShowLaunch(string text, bool showUpdate)
    {
        Visible = true;
        SetCompactActionButtonText(_launchButton, _compact ? CompactLaunchButtonText(text) : text);
        SetCloudControlsVisible(true);
        ShowLaunchButtons(showUpdate);
        _retryButton.Visible = false;
    }

    internal void ShowRetry()
    {
        Visible = true;
        _retryButton.Visible = true;
        SetCloudControlsVisible(false);
        ShowRetryButtons();
    }

    internal void HideAll()
    {
        Visible = false;
        _retryButton.Visible = false;
        SetCloudControlsVisible(false);
        HideSecondaryButtons();
    }

    internal void SetPushPullDisabled(bool disabled)
    {
        if (disabled)
        {
            ResetCloudPushArm(_pushPullRow.Visible);
        }

        _pushButton.Disabled = disabled;
        _cloudPushToggle.Disabled = disabled;
        _confirmPushButton.Disabled = disabled;
        _pullButton.Disabled = disabled;
    }

    internal void SetUpdateButtonText(string text) => SetCompactActionButtonText(_updateButton, text);

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;

    internal void SetRefreshVersionsButtonDisabled(bool disabled) => _refreshVersionsButton.Disabled = disabled;

    internal Control ReadyScrollTarget => _compact ? _cloudGroup : _launchButton;

    internal Control RetryScrollTarget => _retryButton;

    private void ToggleCloudOptions()
    {
        _cloudOptionsExpanded = !_cloudOptionsExpanded;
        ApplyCloudOptionVisibility(_cloudGroup.Visible);
    }

    private void ApplyCloudOptionVisibility(bool cloudControlsVisible)
    {
        if (_compact && !cloudControlsVisible)
        {
            _cloudOptionsExpanded = false;
        }

        if (_cloudOptionsToggle != null)
        {
            _cloudOptionsToggle.Visible = cloudControlsVisible && _compact;
            UpdateCloudOptionsToggleText();
        }

        var showOptions = cloudControlsVisible && (!_compact || _cloudOptionsExpanded);
        if (_compactCloudOptionsRow != null)
            _compactCloudOptionsRow.Visible = showOptions;
        _localBackupToggle.Visible = showOptions;
        _cloudSyncToggle.Visible = showOptions;
    }

    private void ToggleBranchDetails()
    {
        _branchDetailsExpanded = !_branchDetailsExpanded;
        ApplyBranchControlVisibility();
        UpdateBranchHelpText();
    }

    private void ApplyBranchControlVisibility()
    {
        if (_compact && !_branchControlsAvailable)
        {
            _branchDetailsExpanded = false;
        }

        if (_compact)
        {
            _branchDropdown.Visible = _branchControlsAvailable && _branchDetailsExpanded;
            _branchHelpLabel.Visible = _branchControlsAvailable && _branchDetailsExpanded;
            _branchDetailsToggle.Visible = _branchControlsAvailable;
            return;
        }

        _branchDropdown.Visible = _branchControlsAvailable;
        _branchHelpLabel.Visible = _branchControlsAvailable;
        _branchDetailsToggle.Visible = false;
    }

    private void ToggleCloudSafety()
    {
        _cloudSafetyExpanded = !_cloudSafetyExpanded;
        UpdateBranchHelpText();
    }

    private void OpenCompactCloudSafetyFromReadySummary()
    {
        if (!_compact)
            return;

        _cloudSafetyExpanded = true;
        UpdateBranchHelpText();
    }

    private void UpdateCloudOptionsToggleText()
    {
        if (_cloudOptionsToggle == null)
            return;

        SetCompactActionButtonText(_cloudOptionsToggle, _cloudOptionsExpanded
            ? CompactPlaySyncDrawerText("Hide Save Settings", "Backup and cloud")
            : CompactPlaySyncDrawerText(
                $"Backup {OnOff(_localBackupEnabled)} / Cloud {OnOff(_cloudSyncEnabled)}",
                "Save settings"
            ));
    }

    private void ToggleCloudPush()
    {
        _cloudPushExpanded = !_cloudPushExpanded;
        ResetCloudPushArm();
    }

    private void ApplyCloudPushVisibility(bool showPushButton)
    {
        var cloudVisible = _pushPullRow.Visible;
        if (_compact && !cloudVisible)
        {
            _cloudPushExpanded = false;
        }

        if (_cloudPushToggle != null)
        {
            _cloudPushToggle.Visible = cloudVisible && _compact;
            SetCompactActionButtonText(_cloudPushToggle, _compact
                ? CompactCloudPushToggleText(_cloudPushExpanded)
                : "Push Locked");
        }

        var canShowPush = cloudVisible && (!_compact || _cloudPushExpanded);
        _pushButton.Visible = canShowPush && showPushButton;
        if (!canShowPush)
        {
            _confirmPushButton.Visible = false;
            _pushConfirmationLabel.Visible = false;
        }
    }

    private static string OnOff(bool value)
        => value ? "On" : "Off";

    private string CompactCloudSafetySummary()
        => CompactPlaySyncDrawerText(
            "Save Check",
            $"Get saves first: {SteamGameBranch.CompactDisplayName(_gameBranch, 14)}"
        );

    private string CompactCloudSafetyDetailText()
        => $"Saves for: {SteamGameBranch.CompactDisplayName(_gameBranch, 18)}\n"
            + "Get Steam saves before upload. Upload can overwrite Steam.";

    private static string CompactCloudPullText()
        => CompactPlaySyncDrawerText("Get Steam Saves", "Download to Android");

    private static string CompactCloudPushToggleText(bool expanded)
        => expanded
            ? CompactPlaySyncDrawerText("Hide Upload", "Keep locked")
            : CompactPlaySyncDrawerText("Upload Locked", "Review first");

    private static string CompactCloudPushDangerText()
        => CompactPlaySyncDrawerText("Upload to Steam", "Overwrite cloud");

    private static string CompactCloudPushConfirmText()
        => CompactPlaySyncDrawerText("Confirm Upload", "Overwrite cloud");

    private static string CompactCloudPushWarningText()
        => "Steam Cloud overwrite\nConfirm only after Pull/local saves are verified.";

    private static string CompactRetryButtonText()
        => CompactPlaySyncDrawerText("Try Again", "Restart task");

    private static string CompactLaunchButtonText(string text)
        => CompactPlaySyncDrawerText(CompactLaunchTitle(text), "Ready version");

    private static string CompactLaunchTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Start Game";

        var normalized = text.Trim();
        return string.Equals(normalized, "Start Game", StringComparison.OrdinalIgnoreCase)
            ? "Start Game"
            : normalized;
    }

    private static string CompactPlaySyncDrawerText(string action, string detail)
        => $"{action}\n{detail}";

    private string CompactReadyVersionSummary()
    {
        if (_compactStackedActionRows)
        {
            return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadyStackedSummaryBranchLimit)}\n"
                + "Save Check | Upload locked | no auto cloud upload";
        }

        return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadySummaryBranchLimit)} | Save Check | Upload locked";
    }

    private string CompactReadyVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows
            ? CompactReadyVersionHelpStackedBranchLimit
            : CompactReadyVersionHelpBranchLimit;

        return $"Play version: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactReadyFileScope(_gameBranch)}\n"
            + $"{LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches)} | Saves: Get/Upload";
    }

    private static string CompactReadyFileScope(string branch)
        => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "Default files"
            : "Separate files";

    private static void ApplyReadyVersionSummaryButtonStyle(Button button, float scale, bool compact)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildReadyVersionSummaryStyle(scale, compact)
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.045f, 0.085f, 0.095f, 0.95f),
                new Color(0.04f, 0.72f, 0.8f, 0.78f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.05f, 0.06f, 0.98f),
                new Color(0.95f, 0.42f, 0.08f, 0.72f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildReadyVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.04f, 0.048f, 0.58f),
                new Color(0.05f, 0.22f, 0.26f, 0.32f)
            )
        );
    }

    private static StyleBoxFlat BuildReadyVersionSummaryStyle(float scale, bool compact)
        => BuildReadyVersionSummaryStyle(
            scale,
            compact,
            new Color(0.035f, 0.065f, 0.075f, 0.9f),
            new Color(0.04f, 0.55f, 0.62f, 0.65f)
        );

    private static StyleBoxFlat BuildReadyVersionSummaryStyle(
        float scale,
        bool compact,
        Color body,
        Color border
    )
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.CompactVersionSummaryRadius : 8,
                scale
            )
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 9,
            scale
        );
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 10,
            scale
        );
        return style;
    }
}
