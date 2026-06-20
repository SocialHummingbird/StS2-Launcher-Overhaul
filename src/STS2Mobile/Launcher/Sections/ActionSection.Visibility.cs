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
        ApplyCloudOptionVisibility(visible);
        _pushPullRow.Visible = visible;
        if (!visible)
        {
            _cloudPushExpanded = false;
            _cloudSafetyExpanded = false;
        }
        ResetCloudPushArm(visible);
        UpdateBranchHelpText();
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
        _branchControlsAvailable = visibility.Branch;
        ApplyBranchControlVisibility();
        SetSupportButtonsVisible(visibility.Support);
        _safeLaunchButton.Visible = visibility.SafeLaunch;
        _launchButton.Visible = visibility.Launch;
        _readyVersionSummaryPanel.Visible = _compact && visibility.Launch;
    }

    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        SetCompactActionButtonText(_updateButton, _compact
            ? CompactSupportToolText("UPDATES", "Check files")
            : "CHECK FOR UPDATES");
    }

    private void SetSupportButtonsVisible(bool visible)
    {
        _supportToggle.Visible = visible;
        if (!visible)
        {
            _supportExpanded = false;
            _supportGroup.Visible = false;
            SetCompactActionButtonText(_supportToggle, SupportToggleText());
        }
        else
        {
            _supportGroup.Visible = _supportExpanded;
        }
        _diagnosticsButton.Visible = visible;
        _refreshVersionsButton.Visible = visible;
        _clearCachedVersionsButton.Visible = visible;
        _showLastErrorButton.Visible = visible;
        _copyRawLogButton.Visible = visible;
    }
}
