using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool ShouldSuppressSessionUpdate(SessionState state)
        => _model.ShouldSuppressSessionUpdate(state, _updateCheckRunning);
}
