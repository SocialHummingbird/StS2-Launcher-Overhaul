namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct SecondaryButtonVisibility
    {
        private SecondaryButtonVisibility(
            bool update,
            bool redownload,
            bool branch,
            bool support,
            bool safeLaunch,
            bool launch
        )
        {
            Update = update;
            Redownload = redownload;
            Branch = branch;
            Support = support;
            SafeLaunch = safeLaunch;
            Launch = launch;
        }

        internal bool Update { get; }
        internal bool Redownload { get; }
        internal bool Branch { get; }
        internal bool Support { get; }
        internal bool SafeLaunch { get; }
        internal bool Launch { get; }

        internal static SecondaryButtonVisibility LaunchReady(bool showUpdate)
            => new(
                update: showUpdate,
                redownload: true,
                branch: true,
                support: true,
                safeLaunch: true,
                launch: true
            );

        internal static SecondaryButtonVisibility Retry()
            => new(
                update: false,
                redownload: false,
                branch: false,
                support: true,
                safeLaunch: false,
                launch: false
            );

        internal static SecondaryButtonVisibility Hidden()
            => new(
                update: false,
                redownload: false,
                branch: false,
                support: false,
                safeLaunch: false,
                launch: false
            );
    }

    private void SetCloudControlsVisible(bool visible)
    {
        _cloudGroup.Visible = visible;
        _localBackupToggle.Visible = visible;
        _cloudSyncToggle.Visible = visible;
        _pushPullRow.Visible = visible;
        ResetCloudPushArm(visible);
    }

    private void ShowLaunchButtons(bool showUpdate)
        => SetSecondaryButtonsVisible(SecondaryButtonVisibility.LaunchReady(showUpdate));

    private void ShowRetryButtons()
        => SetSecondaryButtonsVisible(SecondaryButtonVisibility.Retry());

    private void HideSecondaryButtons()
        => SetSecondaryButtonsVisible(SecondaryButtonVisibility.Hidden());

    private void SetSecondaryButtonsVisible(SecondaryButtonVisibility visibility)
    {
        ShowUpdateButton(visibility.Update);
        _redownloadButton.Visible = visibility.Redownload;
        _branchButton.Visible = visibility.Branch;
        _branchHelpLabel.Visible = visibility.Branch;
        SetSupportButtonsVisible(visibility.Support);
        _safeLaunchButton.Visible = visibility.SafeLaunch;
        _launchButton.Visible = visibility.Launch;
    }

    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        _updateButton.Text = "CHECK FOR UPDATES";
    }

    private void SetSupportButtonsVisible(bool visible)
    {
        _supportToggle.Visible = visible;
        if (!visible)
        {
            _supportExpanded = false;
            _supportGroup.Visible = false;
            _supportToggle.Text = "MORE SUPPORT OPTIONS";
        }
        else
        {
            _supportGroup.Visible = _supportExpanded;
        }
        _diagnosticsButton.Visible = visible;
        _clearCachedVersionsButton.Visible = visible;
        _showLastErrorButton.Visible = visible;
        _copyRawLogButton.Visible = visible;
    }
}
