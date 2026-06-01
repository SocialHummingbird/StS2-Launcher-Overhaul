using System;
using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection : VBoxContainer
{
    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<bool> LocalBackupToggled;
    internal event Action<bool> CloudSyncToggled;
    internal event Action CloudPushPressed;
    internal event Action CloudPullPressed;
    internal event Action CheckForUpdatesPressed;
    internal event Action RedownloadPressed;
    internal event Action DiagnosticsPressed;
    internal event Action ShowLastErrorPressed;
    internal event Action CopyRawLogPressed;
    internal event Action SafeLaunchPressed;

    private readonly Button _launchButton;
    private readonly Button _safeLaunchButton;
    private readonly Button _retryButton;
    private readonly Button _localBackupToggle;
    private readonly Button _cloudSyncToggle;
    private readonly Button _pushButton;
    private readonly Button _pullButton;
    private readonly Button _updateButton;
    private readonly Button _redownloadButton;
    private readonly Button _diagnosticsButton;
    private readonly Button _showLastErrorButton;
    private readonly Button _copyRawLogButton;
    private readonly HBoxContainer _pushPullRow;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;

    internal void SetLocalBackupChecked(bool value)
    {
        _localBackupToggle.ButtonPressed = value;
        ApplyLocalBackupToggle(value);
    }

    internal void SetCloudSyncChecked(bool value)
    {
        _cloudSyncToggle.ButtonPressed = value;
        ApplyCloudSyncToggle(value);
    }

    internal void ShowLaunch(string text, bool showCloudSync, bool showUpdate)
    {
        _launchButton.Text = text;
        _launchButton.Visible = true;
        SetCloudControlsVisible(showCloudSync);
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
        _pushButton.Disabled = disabled;
        _pullButton.Disabled = disabled;
    }

    internal void SetUpdateButtonText(string text) => _updateButton.Text = text;

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;
}
