using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct CloudOperationTimeout
    {
        internal CloudOperationTimeout(string operation, int timeoutMs)
        {
            Operation = operation;
            TimeoutMs = timeoutMs;
        }

        private string Operation { get; }
        private int TimeoutMs { get; }

        internal async Task WaitAsync(Task task)
        {
            await WaitForCompletionAsync(task).ConfigureAwait(false);
            await task.ConfigureAwait(false);
        }

        internal async Task<T> WaitAsync<T>(Task<T> task)
        {
            await WaitForCompletionAsync(task).ConfigureAwait(false);
            return await task.ConfigureAwait(false);
        }

        private async Task WaitForCompletionAsync(Task task)
        {
            var timeout = Task.Delay(TimeoutMs);
            var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
            if (winner == task)
                return;

            var operation = Operation;
            _ = task.ContinueWith(
                t => PatchHelper.Log(LateCompletionMessage(operation, t.Exception)),
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously
            );
            throw new TimeoutException(
                $"{Operation} timed out after {TimeoutMs}ms"
            );
        }
    }

    private static Task WaitForCloudOperationAsync(
        string operation,
        int timeoutMs,
        Task task
    )
        => new CloudOperationTimeout(operation, timeoutMs).WaitAsync(task);

    private static Task<T> WaitForCloudOperationAsync<T>(
        string operation,
        int timeoutMs,
        Task<T> task
    )
        => new CloudOperationTimeout(operation, timeoutMs).WaitAsync(task);

    private static string LateCompletionMessage(
        string operation,
        AggregateException exception
    )
        => "[Cloud] Late completion after timeout for "
            + $"'{operation}', result: {exception?.GetBaseException().Message}";
}
