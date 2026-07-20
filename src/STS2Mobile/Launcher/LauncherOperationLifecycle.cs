using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal enum LauncherOperationLifecycleState
{
    Active,
    Paused,
    Destroyed,
}

internal readonly struct LauncherOperationLifecycleSnapshot
{
    internal LauncherOperationLifecycleSnapshot(
        LauncherOperationLifecycleState state,
        Task changed
    )
    {
        State = state;
        Changed = changed;
    }

    internal LauncherOperationLifecycleState State { get; }
    internal Task Changed { get; }
}

internal sealed class LauncherOperationLifecycle
{
    private readonly object _lock = new();
    private TaskCompletionSource<bool> _changed = CreateChangeSignal();
    private LauncherOperationLifecycleState _state =
        LauncherOperationLifecycleState.Active;

    internal LauncherOperationLifecycleSnapshot Capture()
    {
        lock (_lock)
            return new LauncherOperationLifecycleSnapshot(_state, _changed.Task);
    }

    internal bool Pause()
        => Transition(
            LauncherOperationLifecycleState.Active,
            LauncherOperationLifecycleState.Paused
        );

    internal bool Resume()
        => Transition(
            LauncherOperationLifecycleState.Paused,
            LauncherOperationLifecycleState.Active
        );

    internal bool Destroy()
    {
        TaskCompletionSource<bool> changed;
        lock (_lock)
        {
            if (_state == LauncherOperationLifecycleState.Destroyed)
                return false;

            _state = LauncherOperationLifecycleState.Destroyed;
            changed = _changed;
            _changed = CreateChangeSignal();
        }

        changed.TrySetResult(true);
        return true;
    }

    private bool Transition(
        LauncherOperationLifecycleState expected,
        LauncherOperationLifecycleState next
    )
    {
        TaskCompletionSource<bool> changed;
        lock (_lock)
        {
            if (_state != expected)
                return false;

            _state = next;
            changed = _changed;
            _changed = CreateChangeSignal();
        }

        changed.TrySetResult(true);
        return true;
    }

    private static TaskCompletionSource<bool> CreateChangeSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
