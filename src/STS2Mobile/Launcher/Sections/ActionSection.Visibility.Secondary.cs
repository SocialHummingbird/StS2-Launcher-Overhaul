namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
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
}
