using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal enum LauncherStartupStage
{
    Validating,
    Preparing,
    SynchronizingSaves,
    Starting,
    WaitingForMenu,
    WaitingForVisibleFrame,
    Running,
    Failed,
}

// Owns the uncancellable game task even after its foreground wait has ended.
internal sealed class LauncherStartupOperation
{
    private readonly object _gate = new();
    private readonly Action<Exception> _observeFailure;
    private LauncherStartupStage _stage = LauncherStartupStage.Validating;
    private bool _started;
    private Task _work;
    private readonly TaskCompletionSource<bool> _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal LauncherStartupOperation(string attemptId, Action<Exception> observeFailure = null)
    {
        if (string.IsNullOrWhiteSpace(attemptId)) throw new ArgumentException("A launch attempt ID is required.");
        AttemptId = attemptId;
        _observeFailure = observeFailure;
    }

    internal string AttemptId { get; }
    internal LauncherStartupStage Stage { get { lock (_gate) return _stage; } }
    internal bool IsActive => Stage is not LauncherStartupStage.Failed and not LauncherStartupStage.Running;
    internal bool RequiresRestart { get { lock (_gate) return _started && _stage == LauncherStartupStage.Failed; } }
    internal Task Observation { get; private set; } = Task.CompletedTask;

    internal async Task WaitAsync(Task task, LauncherAsyncSignalWaiter waiter, LauncherMonotonicDeadline deadline)
    {
        var outcome = await waiter.WaitForSignalAsync(Task.WhenAny(task, _ended.Task), deadline);
        if (!IsActive) throw new OperationCanceledException("This launch attempt has ended.");
        if (outcome != LauncherAsyncWaitOutcome.Signaled)
        {
            Fail();
            if (outcome == LauncherAsyncWaitOutcome.Destroyed)
                throw new OperationCanceledException("The game window closed during loading.");
            throw new TimeoutException("Game loading stopped responding. Return to the launcher and retry in a fresh process.");
        }
        await task;
    }

    internal void SetStage(LauncherStartupStage stage)
    {
        lock (_gate)
        {
            if (!IsActive) throw new OperationCanceledException("The launch attempt has ended.");
            _stage = stage;
        }
    }

    internal Task StartGame(Func<Task> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        lock (_gate)
        {
            if (_started || !IsActive) throw new InvalidOperationException("Game startup is already owned by this attempt.");
            _started = true;
            _stage = LauncherStartupStage.Starting;
            try { _work = start() ?? throw new InvalidOperationException("Game startup returned no task."); }
            catch (Exception ex) { _work = Task.FromException(ex); }
            Observation = ObserveAsync(_work);
            return _work;
        }
    }

    private async Task ObserveAsync(Task work)
    {
        try { await work.ConfigureAwait(false); }
        catch (Exception ex)
        {
            // Logging must never create another unobserved fault.
            try { _observeFailure?.Invoke(ex); } catch { }
        }
    }

    internal void Fail()
    {
        lock (_gate)
            if (_stage != LauncherStartupStage.Running)
            {
                _stage = LauncherStartupStage.Failed;
                _ended.TrySetResult(true);
            }
    }

    internal bool Complete()
    {
        lock (_gate)
        {
            if (!IsActive || _work?.IsCompletedSuccessfully != true) return false;
            _stage = LauncherStartupStage.Running;
            _ended.TrySetResult(true);
            return true;
        }
    }
}
