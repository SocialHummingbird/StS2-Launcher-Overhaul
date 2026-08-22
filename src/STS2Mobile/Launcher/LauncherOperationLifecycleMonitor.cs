using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherOperationLifecycleMonitor : Node
{
    private readonly LauncherOperationLifecycle _lifecycle;
    private readonly LauncherMonotonicDeadline _deadline;
    private readonly Action _stateNotification;

    internal LauncherOperationLifecycleMonitor(
        LauncherOperationLifecycle lifecycle,
        LauncherMonotonicDeadline deadline = null,
        Action stateNotification = null
    )
    {
        _lifecycle = lifecycle;
        _deadline = deadline;
        _stateNotification = stateNotification;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Notification(int what)
    {
        var stateChanged = false;
        if (what == NotificationApplicationPaused)
        {
            ObserveApplicationActive(active: false);
            stateChanged = true;
        }
        else if (what == NotificationApplicationResumed)
        {
            ObserveApplicationActive(active: true);
            stateChanged = true;
        }
        else if (
            what == NotificationApplicationFocusIn
            || what == NotificationApplicationFocusOut
            || what == NotificationWMWindowFocusIn
            || what == NotificationWMWindowFocusOut
        )
        {
            stateChanged = true;
        }

        if (stateChanged)
            _stateNotification?.Invoke();
    }

    internal void ObserveApplicationActive(bool active)
    {
        if (active)
        {
            _deadline?.Resume();
            _lifecycle.Resume();
            return;
        }

        _deadline?.Pause();
        _lifecycle.Pause();
    }

    public override void _ExitTree()
        => _lifecycle.Destroy();
}
