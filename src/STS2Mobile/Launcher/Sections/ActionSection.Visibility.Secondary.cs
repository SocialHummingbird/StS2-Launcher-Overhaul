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
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: update");
        ShowUpdateButton(visibility.Update);
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: redownload");
        _redownloadButton.Visible = visibility.Redownload;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: branch");
        _branchControlsAvailable = visibility.Branch;
        ApplyBranchControlVisibility();
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: mods");
        SetModsControlsVisible(visibility.Launch);
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: support");
        SetSupportButtonsVisible(visibility.Support);
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: safe launch");
        _safeLaunchButton.Visible = visibility.SafeLaunch;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: launch");
        _launchButton.Visible = visibility.Launch;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: disabled state");
        ApplyLaunchControlsDisabled();
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: ready summary");
        _readyVersionSummaryPanel.Visible = _compact && visibility.Launch;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase complete");
    }
}
