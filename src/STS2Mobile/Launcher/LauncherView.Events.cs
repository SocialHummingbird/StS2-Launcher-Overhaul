using System;

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
}
