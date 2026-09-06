using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class LauncherTimeout
{
    internal static async Task<bool> CompletesWithinAsync(Task task, int timeoutMs)
        => await Task.WhenAny(task, Task.Delay(timeoutMs)) == task;

    internal static async Task RunOrThrowAsync(
        Task task,
        int timeoutMs,
        string timeoutMessage
    )
    {
        if (!await CompletesWithinAsync(task, timeoutMs))
            throw new TimeoutException(timeoutMessage);

        await task;
    }

    internal static async Task<T> RunOrThrowAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        int timeoutMs,
        string timeoutMessage
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (timeoutMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));

        using var timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
        timeoutCancellation.CancelAfter(timeoutMs);

        try
        {
            return await operation(timeoutCancellation.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (
                !cancellationToken.IsCancellationRequested
                && timeoutCancellation.IsCancellationRequested
            )
        {
            throw new TimeoutException(timeoutMessage, ex);
        }
    }

}
