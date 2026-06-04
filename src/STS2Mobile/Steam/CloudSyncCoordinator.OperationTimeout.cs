using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task WaitForCloudOperationAsync(
        string operation,
        int timeoutMs,
        Task task
    )
    {
        await WaitForCloudOperationCompletionAsync(
            operation,
            timeoutMs,
            task
        ).ConfigureAwait(false);
        await task.ConfigureAwait(false);
    }

    private static async Task<T> WaitForCloudOperationAsync<T>(
        string operation,
        int timeoutMs,
        Task<T> task
    )
    {
        await WaitForCloudOperationCompletionAsync(
            operation,
            timeoutMs,
            task
        ).ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }

    private static async Task WaitForCloudOperationCompletionAsync(
        string operation,
        int timeoutMs,
        Task task
    )
    {
        var timeout = Task.Delay(timeoutMs);
        var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
        if (winner == task)
            return;

        _ = task.ContinueWith(
            t => PatchHelper.Log(LateCompletionMessage(operation, t.Exception)),
            TaskContinuationOptions.OnlyOnFaulted
                | TaskContinuationOptions.ExecuteSynchronously
        );
        throw new TimeoutException($"{operation} timed out after {timeoutMs}ms");
    }

    private static string LateCompletionMessage(
        string operation,
        AggregateException exception
    )
        => "[Cloud] Late completion after timeout for "
            + $"'{operation}', result: {exception?.GetBaseException().Message}";
}
