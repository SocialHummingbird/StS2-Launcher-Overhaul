namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireViewEvents()
    {
        _view.Login.LoginRequested += LoginPressed;
        _view.Code.CodeSubmitted += CodeSubmitPressed;
        _view.Download.DownloadRequested += DownloadPressed;
        _view.Actions.LaunchPressed += LaunchPressed;
        _view.Actions.RetryPressed += RetryPressed;
        _view.Actions.LocalBackupToggled += LocalBackupToggled;
        _view.Actions.CloudSyncToggled += CloudSyncToggled;
        _view.Actions.CloudPushPressed += CloudPushPressed;
        _view.Actions.CloudPullPressed += CloudPullPressed;
        _view.Actions.CheckForUpdatesPressed += RunUpdateCheck;
        _view.Actions.RedownloadPressed += RedownloadPressed;
        _view.Actions.DiagnosticsPressed += DiagnosticsPressed;
        _view.Actions.ShowLastErrorPressed += ShowLastErrorPressed;
        _view.Actions.CopyRawLogPressed += CopyRawLogPressed;
        _view.Actions.SafeLaunchPressed += SafeLaunchPressed;
    }
}
