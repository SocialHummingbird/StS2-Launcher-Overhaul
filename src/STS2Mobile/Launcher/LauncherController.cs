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

    internal bool Start()
    {
        LauncherLaunchMarkers.RecordPhase("launcher controller wiring", "Wire model events");
        STS2Mobile.PatchHelper.Log("Launcher controller phase: wire model events");
        WireModelEvents();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: wire model events");
        LauncherLaunchMarkers.RecordPhase("launcher controller wiring", "Wire view events");
        STS2Mobile.PatchHelper.Log("Launcher controller phase: wire view events");
        WireViewEvents();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: wire view events");
        LauncherLaunchMarkers.RecordPhase("launcher preferences initialize");
        STS2Mobile.PatchHelper.Log("Launcher controller phase: initialize action preferences");
        InitializeActionPreferences();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: initialize action preferences");
        LauncherLaunchMarkers.RecordPhase("launcher session flow start");
        STS2Mobile.PatchHelper.Log("Launcher controller phase: start session flow");
        StartSessionFlow();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: start session flow");
        return TryStartAutomation();
    }
}
