using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    // Updates visible sections and status text based on session state transitions.
    private void UpdateUI(SessionState state)
    {
        if (ShouldSuppressSessionUpdate(state))
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
}
