using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void WireEvents(
        Action<string, string> loginRequested,
        Action<string> codeSubmitted,
        Action downloadRequested,
        Action launchPressed,
        Action retryPressed,
        Action<bool> localBackupToggled,
        Action<bool> cloudSyncToggled,
        Action cloudPushPressed,
        Action cloudPullPressed,
        Action checkForUpdatesPressed,
        Action redownloadPressed,
        Action diagnosticsPressed,
        Action showLastErrorPressed,
        Action copyRawLogPressed,
        Action safeLaunchPressed
    )
    {
        Login.LoginRequested += loginRequested;
        Code.CodeSubmitted += codeSubmitted;
        Download.DownloadRequested += downloadRequested;
        Actions.LaunchPressed += launchPressed;
        Actions.RetryPressed += retryPressed;
        Actions.LocalBackupToggled += localBackupToggled;
        Actions.CloudSyncToggled += cloudSyncToggled;
        Actions.CloudPushPressed += cloudPushPressed;
        Actions.CloudPullPressed += cloudPullPressed;
        Actions.CheckForUpdatesPressed += checkForUpdatesPressed;
        Actions.RedownloadPressed += redownloadPressed;
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
    }

    internal void SetPushPullDisabled(bool disabled)
        => Actions.SetPushPullDisabled(disabled);

    internal void SetUpdateCheckBusy(bool busy)
    {
        Actions.SetUpdateButtonDisabled(busy);
        if (busy)
            Actions.SetUpdateButtonText("Checking...");
    }

    internal void SetUpdateButtonText(string text)
        => Actions.SetUpdateButtonText(text);
}
