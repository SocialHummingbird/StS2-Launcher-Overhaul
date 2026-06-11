namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private enum LaunchUpdateAction
    {
        Hidden,
        Visible,
    }

    private void ShowLoggedIn()
    {
        _model.MarkConnectionResolved();
        if (LauncherGameFiles.Ready())
        {
            ShowReadyToLaunch(SelectedVersionReadyStatus(), LaunchUpdateAction.Visible);
            return;
        }

        _view.SetStatus(SelectedVersionDownloadRequiredStatus());
        ShowDownloadReadyAction();
    }

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus(_model.FailureStatus());
        SetLoginFormVisible(true, disabled: false);
    }

    private void ShowReadyToLaunch(string status, LaunchUpdateAction updateAction)
    {
        _view.SetStatus(status);
        ShowPreviousLaunchWarningIfNeeded();
        ShowLaunchActions(updateAction);
    }

    private void ShowLaunchActions(LaunchUpdateAction updateAction)
    {
        _view.ShowLaunchActions(
            _model.LaunchButtonText(),
            updateAction == LaunchUpdateAction.Visible
        );
    }
}
