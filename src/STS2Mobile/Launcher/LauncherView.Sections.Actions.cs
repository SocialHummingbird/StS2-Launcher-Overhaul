namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void HideActions()
    {
        SetFirstRunGuideVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("Start here", FirstRunGuide, "Setup guide");
        Actions.HideAll();
        ScrollCompactPrimaryTo(FirstRunGuide);
    }

    internal void ShowRetry()
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactWorkflowStep(CompactWorkflowStep.Play);
        SetCompactCurrentTask("Retry", Actions.RetryScrollTarget, "Restart safely");
        Actions.ShowRetry();
        ScrollCompactPrimaryTo(Actions.RetryScrollTarget);
    }

    internal void ShowLaunchActions(
        string launchText,
        bool showUpdate
    )
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(false);
        SetCompactWorkflowStep(CompactWorkflowStep.Play);
        Actions.ShowLaunch(launchText, showUpdate);
        SetCompactCurrentTask("Play", Actions.ReadyScrollTarget, "Play and saves");
        ScrollCompactPrimaryTo(Actions.ReadyScrollTarget);
    }

    private void SetCompactReadyInstallSectionVisible(bool visible)
    {
        if (!_profile.Compact)
            return;

        Download.Visible = visible;
    }

    internal void SetActionPreferences(LauncherPreferences.ActionPreferences preferences)
    {
        Actions.SetLocalBackupChecked(preferences.LocalBackupEnabled);
        Actions.SetCloudSyncChecked(preferences.CloudSyncEnabled);
        SetGameBranch(preferences.GameBranch);
    }

    internal void SetPushPullDisabled(bool disabled)
        => Actions.SetPushPullDisabled(disabled);

    internal void SetWorkshopButtonsDisabled(bool disabled)
        => Actions.SetWorkshopButtonsDisabled(disabled);

    internal void SetUpdateCheckBusy(bool busy)
    {
        Actions.SetUpdateButtonDisabled(busy);
        if (busy)
            Actions.SetUpdateButtonText("Checking...");
    }

    internal void SetUpdateButtonText(string text)
        => Actions.SetUpdateButtonText(text);
}
