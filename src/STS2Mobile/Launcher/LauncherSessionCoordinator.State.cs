using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    // Updates visible sections and status text based on session state transitions.
    internal void UpdateUI(SessionState state)
    {
        UpdateHomeAccountState(state);
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
    {
        UpdateHomeGameState(readiness: null);
        _launch.ShowLoggedIn(_downloads.ShowDownloadReadyAction);
    }

    private void ShowFailed()
    {
        _model.MarkConnectionResolved();
        _view.SetStatus(_model.FailureStatus());
        SetLoginFormVisible(true, disabled: false);
    }

    private void UpdateHomeState(LauncherModel.FastPathResult result)
    {
        _view.SetHomeAccountState(result.Outcome switch
        {
            LauncherModel.FastPathOutcome.ReadyToLaunch =>
                _model.SteamAccountStatus(),
            LauncherModel.FastPathOutcome.AutoConnect =>
                "Connecting to Steam",
            _ => "Sign in required",
        });
        UpdateHomeGameState(result.Readiness);
    }

    private void UpdateHomeAccountState(SessionState state)
    {
        _view.SetHomeAccountState(state switch
        {
            SessionState.Connecting => "Connecting to Steam",
            SessionState.Authenticating => "Signing in",
            SessionState.VerifyingOwnership => "Verifying ownership",
            SessionState.LoggedIn => _model.SteamAccountStatus(),
            SessionState.Failed => "Steam unavailable",
            _ => "Sign in required",
        });
    }

    private void UpdateHomeGameState(LauncherLaunchReadiness readiness)
    {
        readiness ??= LauncherLaunchReadiness.EvaluateDownloadedState(
            _model.DataDir,
            LauncherPreferences.ReadGameBranch(),
            "home game state"
        );
        var version = SteamGameBranch.DisplayName(readiness.Branch);
        _view.SetHomeGameState(
            readiness.Ready
                ? $"{version} installed"
                : "Download required"
        );
    }

    internal void RefreshHomeGameState()
        => UpdateHomeGameState(readiness: null);
}
