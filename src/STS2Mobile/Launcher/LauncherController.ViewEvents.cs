namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireViewEvents()
    {
        _view.WireEvents(
            LoginPressed,
            CodeSubmitPressed,
            DownloadPressed,
            LaunchPressed,
            RetryPressed,
            LocalBackupToggled,
            CloudSyncToggled,
            CloudPushPressed,
            CloudPullPressed,
            RunUpdateCheck,
            RedownloadPressed,
            DiagnosticsPressed,
            ShowLastErrorPressed,
            CopyRawLogPressed,
            SafeLaunchPressed
        );
    }
}
