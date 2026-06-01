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

        var status = state switch
        {
            SessionState.Connecting => "Connecting to Steam...",
            SessionState.Authenticating => "Authenticating...",
            SessionState.VerifyingOwnership => "Verifying game ownership...",
            _ => null,
        };
        if (status != null)
        {
            _view.SetStatus(status);
            return;
        }

        if (state is SessionState.Disconnected)
        {
            ShowLogin();
            return;
        }

        switch (state)
        {
            case SessionState.LoggedIn:
                ShowLoggedIn();
                break;

            case SessionState.Failed:
                ShowFailed();
                break;
        }
    }
}
