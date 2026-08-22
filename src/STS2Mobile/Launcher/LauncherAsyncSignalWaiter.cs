using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal enum LauncherAsyncWaitOutcome
{
    Signaled,
    DeadlineExceeded,
    Destroyed,
}

internal interface ILauncherAsyncSignalSource
{
    Task WaitForProcessFrameAsync();
    Task WaitForFramePostDrawAsync();
    Task WaitForDelayAsync(int milliseconds, CancellationToken cancellationToken);
}

internal sealed class LauncherAsyncSignalWaiter
{
    private readonly ILauncherAsyncSignalSource _signals;
    private readonly LauncherOperationLifecycle _lifecycle;

    internal LauncherAsyncSignalWaiter(
        ILauncherAsyncSignalSource signals,
        LauncherOperationLifecycle lifecycle = null
    )
    {
        _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        _lifecycle = lifecycle ?? new LauncherOperationLifecycle();
    }

    internal Task<LauncherAsyncWaitOutcome> WaitForProcessFrameAsync(
        LauncherMonotonicDeadline deadline
    )
        => WaitForSignalAsync(_signals.WaitForProcessFrameAsync, deadline);

    internal Task<LauncherAsyncWaitOutcome> WaitForFramePostDrawAsync(
        LauncherMonotonicDeadline deadline
    )
        => WaitForSignalAsync(_signals.WaitForFramePostDrawAsync, deadline);

    internal Task<LauncherAsyncWaitOutcome> WaitForDelayAsync(
        int milliseconds,
        LauncherMonotonicDeadline deadline
    )
    {
        var boundedDelay = Math.Min(
            Math.Max(0, milliseconds),
            deadline.RemainingDelayMilliseconds
        );
        return WaitForSignalAsync(
            () => _signals.WaitForDelayAsync(
                boundedDelay,
                CancellationToken.None
            ),
            deadline
        );
    }

    internal Task<LauncherAsyncWaitOutcome> WaitForSignalAsync(
        Task signal,
        LauncherMonotonicDeadline deadline
    )
    {
        ArgumentNullException.ThrowIfNull(signal);
        return WaitForSignalAsync(() => signal, deadline);
    }

    private async Task<LauncherAsyncWaitOutcome> WaitForSignalAsync(
        Func<Task> createSignal,
        LauncherMonotonicDeadline deadline
    )
    {
        while (true)
        {
            var active = await WaitUntilActiveAsync(deadline);
            if (active != LauncherAsyncWaitOutcome.Signaled)
                return active;

            var lifecycle = _lifecycle.Capture();
            if (lifecycle.State != LauncherOperationLifecycleState.Active)
                continue;

            var signal = createSignal();
            var remaining = deadline.RemainingDelayMilliseconds;
            if (remaining <= 0)
                return LauncherAsyncWaitOutcome.DeadlineExceeded;

            using var timeoutCancellation = new CancellationTokenSource();
            var timeout = _signals.WaitForDelayAsync(
                remaining,
                timeoutCancellation.Token
            );
            var completed = await Task.WhenAny(signal, timeout, lifecycle.Changed);
            if (completed == lifecycle.Changed)
            {
                timeoutCancellation.Cancel();
                _ = ObserveAbandonedSignalAsync(signal);
                continue;
            }

            if (completed == timeout)
            {
                _ = ObserveAbandonedSignalAsync(signal);
                return LauncherAsyncWaitOutcome.DeadlineExceeded;
            }

            timeoutCancellation.Cancel();
            await signal;
            if (deadline.IsExpired)
                return LauncherAsyncWaitOutcome.DeadlineExceeded;

            var afterSignal = _lifecycle.Capture().State;
            if (afterSignal == LauncherOperationLifecycleState.Destroyed)
                return LauncherAsyncWaitOutcome.Destroyed;
            if (afterSignal == LauncherOperationLifecycleState.Paused)
                continue;

            return LauncherAsyncWaitOutcome.Signaled;
        }
    }

    private async Task<LauncherAsyncWaitOutcome> WaitUntilActiveAsync(
        LauncherMonotonicDeadline deadline
    )
    {
        while (true)
        {
            if (deadline.IsExpired)
                return LauncherAsyncWaitOutcome.DeadlineExceeded;

            var lifecycle = _lifecycle.Capture();
            if (lifecycle.State == LauncherOperationLifecycleState.Destroyed)
                return LauncherAsyncWaitOutcome.Destroyed;
            if (lifecycle.State == LauncherOperationLifecycleState.Active)
                return LauncherAsyncWaitOutcome.Signaled;

            if (deadline.IsPaused)
            {
                await lifecycle.Changed;
                continue;
            }

            var remaining = deadline.RemainingDelayMilliseconds;
            using var timeoutCancellation = new CancellationTokenSource();
            var timeout = _signals.WaitForDelayAsync(
                remaining,
                timeoutCancellation.Token
            );
            var completed = await Task.WhenAny(lifecycle.Changed, timeout);
            if (completed == timeout)
                return LauncherAsyncWaitOutcome.DeadlineExceeded;

            timeoutCancellation.Cancel();
        }
    }

    private static async Task ObserveAbandonedSignalAsync(Task signal)
    {
        try
        {
            await signal;
        }
        catch
        {
            // The owning operation has already timed out or left the scene tree.
        }
    }
}
