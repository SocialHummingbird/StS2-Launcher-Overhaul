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
            LaunchPressed,
            SessionRetryPressed,
            _updates.RunUpdateCheck,
            _downloads.UpdateSelectedVersionPressed,
            _versions.RunBranchCatalogRefresh,
            _downloads.RedownloadPressed,
            _diagnostics.DiagnosticsPressed,
            _diagnostics.ShowLastErrorPressed,
            _diagnostics.CopyRawLogPressed,
            SafeLaunchPressed,
            SaveSyncNowPressed,
            SavePullPressed,
            SavePushPressed,
            _workshop.SyncPressed,
            _workshop.ClearPressed,
            RefreshModsPresentation,
            _diagnostics.ReportBugPressed,
            _updates.RunAppUpdateCheck
        );
        _view.DestinationSelected += OnDestinationSelected;
    }
}
