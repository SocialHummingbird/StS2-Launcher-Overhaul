namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void HideActions()
    {
        SelectHomeDestination();
        SetFirstRunGuideVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
        SetCompactCurrentTask("Start here", FirstRunGuide, "Setup guide");
        Actions.HideAll();
        ScrollCompactPrimaryTo(FirstRunGuide);
    }

    internal void ShowRetry()
    {
        SelectHomeDestination();
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
        SelectHomeDestination();
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(false);
        SetCompactWorkflowStep(CompactWorkflowStep.Play);
        Actions.ShowLaunch(launchText, showUpdate);
        SetCompactCurrentTask("Play", Actions.ReadyScrollTarget, "Ready");
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
        Actions.SetRendererMode(preferences.RendererMode);
        SetGameBranch(preferences.GameBranch);
    }

    internal void SetActionPreferences(
        LauncherPreferences.ActionPreferences preferences,
        System.Collections.Generic.IReadOnlyList<LauncherBranchCatalog.BranchOption> branches
    )
    {
        Actions.SetRendererMode(preferences.RendererMode);
        Download.SetGameBranchOptions(preferences.GameBranch, branches);
        Actions.SetGameBranchOptions(preferences.GameBranch, branches);
    }

    internal void SetWorkshopButtonsDisabled(bool disabled)
        => Actions.SetWorkshopButtonsDisabled(disabled);

    internal void SetLaunchControlsDisabled(bool disabled)
        => Actions.SetLaunchControlsDisabled(disabled);

    internal void SetSaveSyncControlsDisabled(bool disabled)
        => Actions.SetSaveSyncControlsDisabled(disabled);

    internal void SetHomeAccountState(string state)
        => Actions.SetHomeAccountState(state);

    internal void SetHomeGameState(string state)
        => Actions.SetHomeGameState(state);

    internal void SetSaveSyncPresentation(
        string headline,
        string lastSuccess,
        string localState,
        string steamState
    )
        => Actions.SetSaveSyncPresentation(headline, lastSuccess, localState, steamState);

    internal void SetPowerVrCompatibility(bool required)
        => Actions.SetPowerVrCompatibility(required);

    internal void SetRendererMode(string mode)
        => Actions.SetRendererMode(mode);

    internal void SetUpdateCheckBusy(bool busy)
    {
        Actions.SetUpdateButtonDisabled(busy);
        if (busy)
            Actions.SetUpdateButtonText("Checking...");
    }

    internal void SetUpdateButtonText(string text)
        => Actions.SetUpdateButtonText(text);
}
