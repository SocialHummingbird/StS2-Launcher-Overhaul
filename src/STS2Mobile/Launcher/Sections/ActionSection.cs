using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection : VBoxContainer
{
    private const string PushButtonText = "Push to Cloud";
    private const string PushConfirmButtonText = "ARE YOU SURE? PUSH TO CLOUD";

    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<string> GameBranchChanged;
    internal event Action<bool> LocalBackupToggled;
    internal event Action<bool> CloudSyncToggled;
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
    private readonly OptionButton _branchDropdown;
    private readonly Label _branchHelpLabel;
    private readonly Button _localBackupToggle;
    private readonly Button _cloudSyncToggle;
    private readonly Button _pushButton;
    private readonly Button _confirmPushButton;
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
    private readonly VBoxContainer _pushPullRow;
    private readonly Button _supportToggle;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private bool _supportExpanded;
    private string _gameBranch = SteamGameBranch.Public;

    internal void SetLocalBackupChecked(bool value)
        => ApplyLocalBackupToggle(value);

    internal void SetCloudSyncChecked(bool value)
        => ApplyCloudSyncToggle(value);

    internal void SetGameBranch(string branch)
    {
        _gameBranch = SteamGameBranch.Normalize(branch);
        PopulateBranchDropdown();
        _branchHelpLabel.Text = SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
            + "\n"
            + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches);
    }

    internal void SetAvailableBranches(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _availableBranches = branches ?? Array.Empty<LauncherBranchCatalog.BranchOption>();
        PopulateBranchDropdown();
    }

    internal void ShowLaunch(string text, bool showUpdate)
    {
        _launchButton.Text = text;
        SetCloudControlsVisible(true);
        ShowLaunchButtons(showUpdate);
        _retryButton.Visible = false;
    }

    internal void ShowRetry()
    {
        _retryButton.Visible = true;
        SetCloudControlsVisible(false);
        ShowRetryButtons();
    }

    internal void HideAll()
    {
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
        _confirmPushButton.Disabled = disabled;
        _pullButton.Disabled = disabled;
    }

    internal void SetUpdateButtonText(string text) => _updateButton.Text = text;

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;

    internal void SetRefreshVersionsButtonDisabled(bool disabled) => _refreshVersionsButton.Disabled = disabled;
}
