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
            RendererModeChanged,
            _launch.LaunchPressed,
            _session.RetryPressed,
            LocalBackupToggled,
            _cloud.CloudSyncToggled,
            _cloud.EvaluateCloudPushEligibility,
            _cloud.CloudPushPressed,
            _cloud.CloudPullPressed,
            _cloud.CloudOperationCancelPressed,
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

    private void LocalBackupToggled(bool pressed)
    {
        var result = _session.LocalBackupToggled(pressed);
        _cloud.LocalBackupRecoveryCompleted(result);
    }
}
