namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void ShowLoggedIn()
    {
        _model.MarkConnectionResolved();
        if (LauncherGameFiles.Ready())
        {
            ShowReadyToLaunch(_model.LoggedInStatus(), showUpdate: true);
            return;
        }

        _view.SetStatus(_model.LoggedInStatus());
        _view.ShowDownloadAction("DOWNLOAD GAME FILES");
        _view.SetDownloadButtonDisabled(false);
    }

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus(_model.FailureStatus());
        SetLoginFormVisible(true, disabled: false);
    }

    private void ShowReadyToLaunch(string status, bool showUpdate)
    {
        _view.SetStatus(status);
        ShowPreviousLaunchWarningIfNeeded();
        ShowLaunchActions(showUpdate);
    }

    private void ShowLaunchActions(bool showUpdate)
    {
        _view.ShowLaunchActions(
            _model.LaunchButtonText(),
            showCloudSync: true,
            showUpdate
        );
    }
}
