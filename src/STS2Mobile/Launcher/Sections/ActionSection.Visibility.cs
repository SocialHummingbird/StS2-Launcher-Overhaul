namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void SetCloudControlsVisible(bool visible)
    {
        _localBackupToggle.Visible = visible;
        _cloudSyncToggle.Visible = visible;
        _pushPullRow.Visible = visible;
    }

    private void ShowLaunchButtons(bool showUpdate)
        => SetSecondaryButtonsVisible(
            showUpdate,
            showRedownload: true,
            showSupport: true,
            showSafeLaunch: true
        );

    private void ShowRetryButtons()
        => SetSecondaryButtonsVisible(
            showUpdate: false,
            showRedownload: false,
            showSupport: true,
            showSafeLaunch: false,
            showLaunch: false
        );

    private void HideSecondaryButtons()
        => SetSecondaryButtonsVisible(
            showUpdate: false,
            showRedownload: false,
            showSupport: false,
            showSafeLaunch: false,
            showLaunch: false
        );

    private void SetSecondaryButtonsVisible(
        bool showUpdate,
        bool showRedownload,
        bool showSupport,
        bool showSafeLaunch,
        bool showLaunch = true
    )
    {
        ShowUpdateButton(showUpdate);
        _redownloadButton.Visible = showRedownload;
        SetSupportButtonsVisible(showSupport);
        _safeLaunchButton.Visible = showSafeLaunch;
        _launchButton.Visible = showLaunch;
    }

    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        _updateButton.Text = "CHECK FOR UPDATES";
    }

    private void SetSupportButtonsVisible(bool visible)
    {
        _diagnosticsButton.Visible = visible;
        _showLastErrorButton.Visible = visible;
        _copyRawLogButton.Visible = visible;
    }
}
