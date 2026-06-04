using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct CloudOperationTimeout
    {
        private readonly string _operation;
        private readonly int _timeoutMs;

        private CloudOperationTimeout(string operation, int timeoutMs)
        {
            _operation = operation;
            _timeoutMs = timeoutMs;
        }

        internal static CloudOperationTimeout For(string operation, int timeoutMs)
            => new(operation, timeoutMs);

        internal async Task WaitAsync(Task task)
        {
            var timeout = Task.Delay(_timeoutMs);
            var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
            if (winner == task)
                return;

            var operation = _operation;
            _ = task.ContinueWith(
                t => PatchHelper.Log(LateCompletionMessage(operation, t.Exception)),
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously
            );
            throw new TimeoutException(
                $"{_operation} timed out after {_timeoutMs}ms"
            );
        }

        private static string LateCompletionMessage(
            string operation,
            AggregateException exception
        )
            => "[Cloud] Late completion after timeout for "
                + $"'{operation}', result: {exception?.GetBaseException().Message}";
    }
}
