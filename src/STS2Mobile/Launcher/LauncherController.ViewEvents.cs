namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireViewEvents()
    {
        _view.WireEvents(
            _session.LoginPressed,
            _session.CodeSubmitPressed,
            _downloads.DownloadPressed,
            _branchSwitch.GameBranchChanged,
            _launch.LaunchPressed,
            _session.RetryPressed,
            _session.LocalBackupToggled,
            _cloud.CloudSyncToggled,
            _cloud.CanArmCloudPush,
            _cloud.CloudPushPressed,
            _cloud.CloudPullPressed,
            _updates.RunUpdateCheck,
            _versions.RunBranchCatalogRefresh,
            _downloads.RedownloadPressed,
            _downloads.ClearCachedVersionsPressed,
            _diagnostics.DiagnosticsPressed,
            _diagnostics.ShowLastErrorPressed,
            _diagnostics.CopyRawLogPressed,
            _launch.SafeLaunchPressed,
            _workshop.SyncPressed,
            _workshop.ClearPressed
        );
    }
}
