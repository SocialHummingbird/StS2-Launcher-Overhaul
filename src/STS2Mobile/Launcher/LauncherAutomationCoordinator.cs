using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherAutomationCoordinator
{
    private const string AutomationFileName = "launcher_automation_action.txt";
    private const string AutomationMarkerFileName = "last_launcher_automation.txt";

    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherVersionCoordinator _versions;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly Action<Action> _runOnMainThread;

    internal LauncherAutomationCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherVersionCoordinator versions,
        LauncherLaunchCoordinator launch,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _versions = versions;
        _launch = launch;
        _runOnMainThread = runOnMainThread;
    }

    internal bool TryStartAutomation()
    {
        var request = LauncherAutomationRequest.TryConsume(_model.DataDir);
        if (!request.HasValue)
            return false;

        _ = RunAutomationAsync(request.Value);
        return true;
    }
}
