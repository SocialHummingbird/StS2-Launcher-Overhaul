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
    {
        ShowUpdateButton(showUpdate);
        _redownloadButton.Visible = true;
        SetSupportButtonsVisible(true);
        _safeLaunchButton.Visible = true;
    }

    private void ShowRetryButtons()
    {
        ShowUpdateButton(false);
        _redownloadButton.Visible = false;
        SetSupportButtonsVisible(true);
        _safeLaunchButton.Visible = false;
        _launchButton.Visible = false;
    }

    private void HideSecondaryButtons()
    {
        ShowUpdateButton(false);
        _redownloadButton.Visible = false;
        SetSupportButtonsVisible(false);
        _safeLaunchButton.Visible = false;
        _launchButton.Visible = false;
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
