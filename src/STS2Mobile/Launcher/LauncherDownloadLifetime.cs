using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

// Keeps replacement work and shared-resource cleanup behind the active worker's drain.
internal sealed class LauncherDownloadLifetime
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _cleanup = new(1, 1);
    private Operation _active;
    private int _pendingCleanup;
    private bool _closed;

    internal bool IsRunning { get { lock (_sync) return _active != null || _pendingCleanup != 0; } }
    internal bool CanPause { get { lock (_sync) return _active?.Pausable == true && _pendingCleanup == 0; } }

    internal Task TryRunAsync(Func<CancellationToken, Task> work, bool pausable)
    {
        Operation operation;
        lock (_sync)
        {
            if (_closed || _active != null || _pendingCleanup != 0)
                return null;
            operation = new Operation(pausable);
            _active = operation;
        }

        return RunAsync(operation, work);
    }

    private async Task RunAsync(Operation operation, Func<CancellationToken, Task> work)
    {
        try
        {
            await work(operation.Cancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_sync)
            {
                operation.Cancellation.Dispose();
                _active = null;
            }
            operation.Drained.TrySetResult(true);
        }
    }

    internal Task PauseAsync()
    {
        lock (_sync)
        {
            var operation = _active;
            if (operation?.Pausable != true)
                return Task.CompletedTask;
            operation.Cancellation.Cancel();
            return operation.Drained.Task;
        }
    }

    internal void EnterLocalRepair()
    {
        lock (_sync)
        {
            _active.Cancellation.Token.ThrowIfCancellationRequested();
            _active.Pausable = false;
        }
    }

    internal async Task DrainThenAsync(Func<Task> afterDrain, bool close = false)
    {
        Task drained;
        lock (_sync)
        {
            if (_closed)
                throw new ObjectDisposedException(nameof(LauncherDownloadLifetime));
            _closed = close;
            _pendingCleanup++;
            var operation = _active;
            drained = operation?.Drained.Task ?? Task.CompletedTask;
            if (operation?.Pausable == true)
                operation.Cancellation.Cancel();
        }

        await _cleanup.WaitAsync().ConfigureAwait(false);
        try
        {
            await drained.ConfigureAwait(false);
            await afterDrain().ConfigureAwait(false);
        }
        finally
        {
            lock (_sync) _pendingCleanup--;
            _cleanup.Release();
        }
    }

    private sealed class Operation
    {
        internal Operation(bool pausable) => Pausable = pausable;
        internal readonly CancellationTokenSource Cancellation = new();
        internal readonly TaskCompletionSource<bool> Drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Pausable;
    }
}
