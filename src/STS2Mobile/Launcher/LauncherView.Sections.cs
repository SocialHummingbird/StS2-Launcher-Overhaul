using System;
using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactScrollAnchorTopPadding = 14;

    internal void WireEvents(
        Action<string, string> loginRequested,
        Action<string> codeSubmitted,
        Action downloadRequested,
        Action<string> gameBranchChanged,
        Action launchPressed,
        Action retryPressed,
        Action<bool> localBackupToggled,
        Action<bool> cloudSyncToggled,
        Func<bool> cloudPushArmRequested,
        Action cloudPushPressed,
        Action cloudPullPressed,
        Action checkForUpdatesPressed,
        Action refreshGameVersionsPressed,
        Action redownloadPressed,
        Action clearCachedVersionsPressed,
        Action diagnosticsPressed,
        Action showLastErrorPressed,
        Action copyRawLogPressed,
        Action safeLaunchPressed
    )
    {
        Login.LoginRequested += loginRequested;
        Code.CodeSubmitted += codeSubmitted;
        Download.DownloadRequested += downloadRequested;
        Download.GameBranchChanged += gameBranchChanged;
        Download.RefreshGameVersionsRequested += refreshGameVersionsPressed;
        Actions.GameBranchChanged += gameBranchChanged;
        Actions.LaunchPressed += launchPressed;
        Actions.RetryPressed += retryPressed;
        Actions.LocalBackupToggled += localBackupToggled;
        Actions.CloudSyncToggled += cloudSyncToggled;
        Actions.CloudPushArmRequested += cloudPushArmRequested;
        Actions.CloudPushPressed += cloudPushPressed;
        Actions.CloudPullPressed += cloudPullPressed;
        Actions.CheckForUpdatesPressed += checkForUpdatesPressed;
        Actions.RefreshGameVersionsPressed += refreshGameVersionsPressed;
        Actions.RedownloadPressed += redownloadPressed;
        Actions.ClearCachedVersionsPressed += clearCachedVersionsPressed;
        Actions.DiagnosticsPressed += diagnosticsPressed;
        Actions.ShowLastErrorPressed += showLastErrorPressed;
        Actions.CopyRawLogPressed += copyRawLogPressed;
        Actions.SafeLaunchPressed += safeLaunchPressed;
    }

    internal void ClearLoginPasswordAndDisable()
    {
        Login.SetDisabled(true);
        Login.ClearPassword();
    }

    internal void SetLoginFormVisible(bool visible, bool disabled)
    {
        if (visible)
        {
            SetFirstRunGuideVisible(false);
            HideCompactCompletedAuthSections(showCode: false);
        }

        Login.SetFormVisible(visible, disabled);
        if (visible)
        {
            SetCompactWorkflowStep(CompactWorkflowStep.SignIn);
            SetCompactCurrentTask("Sign in", Login, "Steam account");
            ScrollCompactPrimaryTo(Login);
        }
    }

    internal void ShowCodePrompt(bool wasIncorrect)
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: true);
        SetCompactWorkflowStep(CompactWorkflowStep.Code);
        SetCompactCurrentTask("Verify", Code, "Steam Guard code");
        Code.Show(wasIncorrect);
        ScrollCompactPrimaryTo(Code);
    }

    internal void ShowDownloadAction(string buttonText)
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.Visible = true;
        Download.Reset(buttonText);
        ScrollCompactPrimaryTo(Download);
    }

    internal void SetGameBranch(string branch)
    {
        Download.SetGameBranch(branch);
        Actions.SetGameBranch(branch);
    }

    internal void SetGameBranchOptions(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        Download.SetAvailableBranches(branches);
        Actions.SetAvailableBranches(branches);
    }

    internal void ShowDownloadProgress(string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.ShowProgress(text);
    }

    internal void SetDownloadProgress(double percentage, string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        Download.SetProgress(percentage, text);
    }

    internal void HideDownload()
        => Download.Visible = false;

    internal void ResetDownload()
        => Download.Reset();

    internal void ResetDownload(string buttonText)
        => Download.Reset(buttonText);

    internal void SetDownloadButtonDisabled(bool disabled)
        => Download.SetButtonDisabled(disabled);

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

    private void SetFirstRunGuideVisible(bool visible)
        => FirstRunGuide.Visible = !_profile.Compact || visible;

    private void SetCompactReadyInstallSectionVisible(bool visible)
    {
        if (!_profile.Compact)
            return;

        Download.Visible = visible;
    }

    private void HideCompactCompletedAuthSections(bool showCode)
    {
        if (!_profile.Compact)
            return;

        Login.SetFormVisible(false, disabled: true);
        Code.Visible = showCode;
    }

    private void ScrollCompactPrimaryTo(Control target)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(target))
            return;

        _compactScrollAnchorTarget = target;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(PrimaryScroll)
                || !GodotObject.IsInstanceValid(target)
                || !PrimaryScroll.IsInsideTree()
                || !target.IsInsideTree()
                || !target.IsVisibleInTree())
            {
                return;
            }

            PrimaryScroll.EnsureControlVisible(target);
            Callable.From(() => ApplyCompactScrollAnchorPadding(target)).CallDeferred();
        }).CallDeferred();
    }

    private void ApplyCompactScrollAnchorPadding(Control target)
    {
        if (!_profile.Compact || !PrimaryScroll.IsInsideTree() || !target.IsInsideTree() || !target.IsVisibleInTree())
            return;

        var scrollTop = PrimaryScroll.GetGlobalRect().Position.Y;
        var targetTop = target.GetGlobalRect().Position.Y;
        var anchoredScroll = PrimaryScroll.ScrollVertical
            + targetTop
            - scrollTop
            - LauncherViewLayoutMetrics.ScaleInt(CompactScrollAnchorTopPadding, _scale);
        PrimaryScroll.ScrollVertical = Math.Max(0, (int)MathF.Round(anchoredScroll));
    }

    internal void SetActionPreferences(LauncherPreferences.ActionPreferences preferences)
    {
        Actions.SetLocalBackupChecked(preferences.LocalBackupEnabled);
        Actions.SetCloudSyncChecked(preferences.CloudSyncEnabled);
        SetGameBranch(preferences.GameBranch);
    }

    internal void SetPushPullDisabled(bool disabled)
        => Actions.SetPushPullDisabled(disabled);

    internal void SetUpdateCheckBusy(bool busy)
    {
        Actions.SetUpdateButtonDisabled(busy);
        if (busy)
            Actions.SetUpdateButtonText("Checking...");
    }

    internal void SetRefreshGameVersionsBusy(bool busy)
    {
        Download.SetRefreshVersionsButtonDisabled(busy);
        Actions.SetRefreshVersionsButtonDisabled(busy);
    }

    internal void SetUpdateButtonText(string text)
        => Actions.SetUpdateButtonText(text);
}
