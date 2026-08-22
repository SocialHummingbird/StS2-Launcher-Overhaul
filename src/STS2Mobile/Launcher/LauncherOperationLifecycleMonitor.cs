using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherOperationLifecycleMonitor : Node
{
    private readonly LauncherOperationLifecycle _lifecycle;
    private readonly LauncherMonotonicDeadline _deadline;

    internal LauncherOperationLifecycleMonitor(
        LauncherOperationLifecycle lifecycle,
        LauncherMonotonicDeadline deadline = null
    )
    {
        _lifecycle = lifecycle;
        _deadline = deadline;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationPaused)
            ObserveApplicationActive(active: false);
        else if (what == NotificationApplicationResumed)
            ObserveApplicationActive(active: true);
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
