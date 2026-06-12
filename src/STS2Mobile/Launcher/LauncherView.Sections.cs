using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void WireEvents(
        Action<string, string> loginRequested,
        Action<string> codeSubmitted,
        Action downloadRequested,
        Action<string> gameBranchChanged,
        Action launchPressed,
        Action retryPressed,
        Action<bool> localBackupToggled,
        Action<bool> cloudSyncToggled,
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
        Login.Visible = visible;
        Login.SetDisabled(disabled);
    }

    internal void ShowCodePrompt(bool wasIncorrect)
        => Code.Show(wasIncorrect);

    internal void ShowDownloadAction(string buttonText)
    {
        Download.Visible = true;
        Download.Reset(buttonText);
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
        => Download.ShowProgress(text);

    internal void SetDownloadProgress(double percentage, string text)
        => Download.SetProgress(percentage, text);

    internal void HideDownload()
        => Download.Visible = false;

    internal void ResetDownload()
        => Download.Reset();

    internal void ResetDownload(string buttonText)
        => Download.Reset(buttonText);

    internal void SetDownloadButtonDisabled(bool disabled)
        => Download.SetButtonDisabled(disabled);

    internal void HideActions()
        => Actions.HideAll();

    internal void ShowRetry()
        => Actions.ShowRetry();

    internal void ShowLaunchActions(
        string launchText,
        bool showUpdate
    )
        => Actions.ShowLaunch(launchText, showUpdate);

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
