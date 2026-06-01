using System;

namespace STS2Mobile.Launcher;

// Wires model events to view updates and handles the launcher UI state machine.
// All model callbacks are marshalled to the main thread before updating the view.
internal sealed partial class LauncherController
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action<Action> _runOnMainThread;

    internal LauncherController(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _runOnMainThread = runOnMainThread;
    }

    internal void Start()
    {
        WireModelEvents();
        WireViewEvents();
        InitializeActionPreferences();
        StartSessionFlow();
    }
}
