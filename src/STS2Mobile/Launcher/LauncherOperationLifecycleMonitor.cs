using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherOperationLifecycleMonitor : Node
{
    private readonly LauncherOperationLifecycle _lifecycle;

    internal LauncherOperationLifecycleMonitor(
        LauncherOperationLifecycle lifecycle
    )
    {
        _lifecycle = lifecycle;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationPaused)
            _lifecycle.Pause();
        else if (what == NotificationApplicationResumed)
            _lifecycle.Resume();
    }

    public override void _ExitTree()
        => _lifecycle.Destroy();
}
