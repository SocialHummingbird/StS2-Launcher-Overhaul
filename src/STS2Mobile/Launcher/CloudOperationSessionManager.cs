#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed class CloudOperationSessionManager : IDisposable
{
    private readonly object _stateLock = new();
    private ActiveOperation? _active;
    private bool _disposed;

    internal bool IsActive
    {
        get
        {
            lock (_stateLock)
                return _active != null;
        }
    }

    internal bool TryRun(
        Func<CancellationToken, Task> run,
        out Task execution
    )
    {
        ArgumentNullException.ThrowIfNull(run);

        ActiveOperation operation;
        lock (_stateLock)
        {
            if (_disposed || _active != null)
            {
                execution = Task.CompletedTask;
                return false;
            }

            operation = new ActiveOperation();
            _active = operation;
        }

        execution = RunOwnedAsync(operation, run);
        return true;
    }

    internal async Task CancelAndDrainAsync()
    {
        ActiveOperation? operation;
        lock (_stateLock)
        {
            operation = _active;
            operation?.Cancel();
        }

        if (operation != null)
            await operation.Drained.ConfigureAwait(false);
    }

    internal void Dispose()
    {
        ActiveOperation? operation;
        lock (_stateLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            operation = _active;
            operation?.Cancel();
        }

        operation?.Drained.GetAwaiter().GetResult();
    }

    void IDisposable.Dispose()
        => Dispose();

    private async Task RunOwnedAsync(
        ActiveOperation operation,
        Func<CancellationToken, Task> run
    )
    {
        try
        {
            await run(operation.CancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_active, operation))
                    _active = null;
            }

            operation.CompleteDrain();
        }
    }

    private sealed class ActiveOperation
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly TaskCompletionSource _drained = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        internal CancellationToken CancellationToken
            => _cancellation.Token;

        internal Task Drained
            => _drained.Task;

        internal void Cancel()
            => _cancellation.Cancel();

        internal void CompleteDrain()
        {
            _cancellation.Dispose();
            _drained.TrySetResult();
        }
    }
}
