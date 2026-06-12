namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireViewEvents()
    {
        _view.WireEvents(
            LoginPressed,
            CodeSubmitPressed,
            DownloadPressed,
            GameBranchChanged,
            LaunchPressed,
            RetryPressed,
            LocalBackupToggled,
            CloudSyncToggled,
            CloudPushPressed,
            CloudPullPressed,
            RunUpdateCheck,
            RunBranchCatalogRefresh,
            RedownloadPressed,
            ClearCachedVersionsPressed,
            DiagnosticsPressed,
            ShowLastErrorPressed,
            CopyRawLogPressed,
            SafeLaunchPressed
        );
    }
}
