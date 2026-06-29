using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action<Action> _runOnMainThread;

    internal LauncherCloudSyncCoordinator(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _runOnMainThread = runOnMainThread;
    }

    internal void CloudSyncToggled(bool pressed)
    {
        LauncherPreferences.SaveCloudSyncEnabled(pressed);
        _view.SetStatus(
            pressed
                ? "Game cloud sync enabled. Manual Push/Pull remains available from the launcher."
                : "Game cloud sync disabled. The game will use Android local saves; manual Push/Pull remains available."
        );
    }
}
