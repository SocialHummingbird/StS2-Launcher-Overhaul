using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    // Updates visible sections and status text based on session state transitions.
    internal void UpdateUI(SessionState state)
    {
        if (_model.ShouldSuppressSessionUpdate(state, _isUpdateCheckRunning()))
            return;

        ShowSessionState(state);
    }

    private void ShowSessionState(SessionState state)
    {
        _view.HideAllSections();

        switch (state)
        {
            case SessionState.Connecting:
                _view.SetStatus("Connecting to Steam...");
                break;

            case SessionState.Authenticating:
                _view.SetStatus("Authenticating...");
                break;

            case SessionState.VerifyingOwnership:
                _view.SetStatus("Verifying game ownership...");
                break;

            case SessionState.Disconnected:
                ShowLogin();
                break;

            case SessionState.LoggedIn:
                ShowLoggedIn();
                break;

            case SessionState.Failed:
                ShowFailed();
                break;
        }
    }

    private void ShowLoggedIn()
        => _launch.ShowLoggedIn(_downloads.ShowDownloadReadyAction);

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus(_model.FailureStatus());
        SetLoginFormVisible(true, disabled: false);
    }
}
