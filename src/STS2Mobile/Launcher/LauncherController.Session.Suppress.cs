using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool ShouldSuppressSessionUpdate(SessionState state)
    {
        if (
            _model.AwaitingCode
            && state
                is SessionState.Connecting
                    or SessionState.Authenticating
        )
            return true;

        if (_updateCheckRunning)
            return true;

        return state == SessionState.Disconnected && _model.ConnectionResolved;
    }
}
