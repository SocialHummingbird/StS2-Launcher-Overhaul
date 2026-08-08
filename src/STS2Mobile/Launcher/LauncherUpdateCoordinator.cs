using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherVersionCoordinator _versions;
    private readonly Action<Action> _runOnMainThread;

    private bool _updateCheckRunning;

    internal LauncherUpdateCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherVersionCoordinator versions,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _versions = versions;
        _runOnMainThread = runOnMainThread;
    }

    internal bool IsRunning => _updateCheckRunning;
}
