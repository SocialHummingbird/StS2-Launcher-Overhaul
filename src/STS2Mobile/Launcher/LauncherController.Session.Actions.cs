namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void ShowLoggedIn()
    {
        _model.MarkConnectionResolved();
        if (LauncherGameFiles.Ready())
        {
            ShowReadyToLaunch($"Logged in as {_model.AccountName}", showUpdate: true);
            return;
        }

        _view.SetStatus($"Logged in as {_model.AccountName}");
        _view.Download.Visible = true;
        _view.Download.SetButtonDisabled(false);
    }

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus($"Error: {_model.FailReason}");
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
        _view.Actions.ShowLaunch(
            _model.InGameMode ? "PLAY" : "START GAME",
            showCloudSync: true,
            showUpdate
        );
    }
}
