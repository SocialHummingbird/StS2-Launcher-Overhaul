using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void WireEvents(
        Action<string, string> loginRequested,
        Action<string> codeSubmitted,
        Action downloadRequested,
        Action<string> gameBranchChanged,
        Action<string> rendererModeChanged,
        Action launchPressed,
        Action retryPressed,
        Action checkForUpdatesPressed,
        Action updateSelectedVersionPressed,
        Action refreshGameVersionsPressed,
        Action redownloadPressed,
        Action diagnosticsPressed,
        Action showLastErrorPressed,
        Action copyRawLogPressed,
        Action safeLaunchPressed,
        Action saveSyncNowPressed,
        Action savePullPressed,
        Action savePushPressed,
        Action workshopSyncPressed,
        Action workshopClearPressed,
        Action modsSelectionChanged = null
    )
    {
        Login.LoginRequested += loginRequested;
        Login.StatusRequested += SetStatus;
        Code.CodeSubmitted += codeSubmitted;
        Download.DownloadRequested += downloadRequested;
        Download.GameBranchChanged += gameBranchChanged;
        Download.RefreshGameVersionsRequested += refreshGameVersionsPressed;
        Actions.GameBranchChanged += gameBranchChanged;
        Actions.RendererModeChanged += rendererModeChanged;
        Actions.LaunchPressed += launchPressed;
        Actions.RetryPressed += retryPressed;
        Actions.CheckForUpdatesPressed += checkForUpdatesPressed;
        Actions.UpdateSelectedVersionPressed += updateSelectedVersionPressed;
        Actions.RefreshGameVersionsPressed += refreshGameVersionsPressed;
        Actions.RedownloadPressed += redownloadPressed;
        Actions.DiagnosticsPressed += diagnosticsPressed;
        Actions.ShowLastErrorPressed += showLastErrorPressed;
        Actions.CopyRawLogPressed += copyRawLogPressed;
        Actions.SafeLaunchPressed += safeLaunchPressed;
        Actions.SaveSyncNowPressed += saveSyncNowPressed;
        Actions.SavePullPressed += savePullPressed;
        Actions.SavePushPressed += savePushPressed;
        Actions.WorkshopSyncPressed += workshopSyncPressed;
        Actions.WorkshopClearPressed += workshopClearPressed;
        if (modsSelectionChanged != null)
            Actions.ModsSelectionChanged += modsSelectionChanged;
    }
}
