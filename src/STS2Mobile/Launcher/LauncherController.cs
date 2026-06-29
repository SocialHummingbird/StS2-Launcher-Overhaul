using System;

namespace STS2Mobile.Launcher;

// Wires model events to view updates and handles the launcher UI state machine.
// All model callbacks are marshalled to the main thread before updating the view.
internal sealed partial class LauncherController
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherWorkshopCoordinator _workshop;
    private readonly LauncherDiagnosticsCoordinator _diagnostics;
    private readonly LauncherVersionCoordinator _versions;
    private readonly LauncherCloudSyncCoordinator _cloud;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly LauncherDownloadCoordinator _downloads;
    private readonly LauncherBranchSwitchCoordinator _branchSwitch;
    private readonly LauncherUpdateCoordinator _updates;
    private readonly LauncherSessionCoordinator _session;
    private readonly LauncherAutomationCoordinator _automation;
    private readonly LauncherStartupCoordinator _startup;
    private readonly Action<Action> _runOnMainThread;

    internal LauncherController(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _workshop = new LauncherWorkshopCoordinator(model, view);
        _diagnostics = new LauncherDiagnosticsCoordinator(model, view);
        _versions = new LauncherVersionCoordinator(model, view);
        _cloud = new LauncherCloudSyncCoordinator(model, view, runOnMainThread);
        _launch = new LauncherLaunchCoordinator(model, view, _diagnostics);
        _downloads = new LauncherDownloadCoordinator(
            model,
            view,
            _launch,
            _versions.RefreshGameBranchOptions
        );
        _branchSwitch = new LauncherBranchSwitchCoordinator(
            model,
            view,
            _versions,
            _launch,
            _downloads
        );
        _updates = new LauncherUpdateCoordinator(
            model,
            view,
            _versions,
            runOnMainThread
        );
        _session = new LauncherSessionCoordinator(
            model,
            view,
            _launch,
            _downloads,
            runOnMainThread,
            () => _updates.IsRunning
        );
        _automation = new LauncherAutomationCoordinator(
            model,
            view,
            _versions,
            _launch,
            runOnMainThread
        );
        _startup = new LauncherStartupCoordinator(view, _versions);
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
        _startup.InitializeActionPreferences();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: initialize action preferences");
        LauncherLaunchMarkers.RecordPhase("launcher session flow start");
        STS2Mobile.PatchHelper.Log("Launcher controller phase: start session flow");
        _session.StartSessionFlow();
        STS2Mobile.PatchHelper.Log("Launcher controller phase complete: start session flow");
        return _automation.TryStartAutomation();
    }
}
