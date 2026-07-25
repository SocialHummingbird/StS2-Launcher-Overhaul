using System;
using System.Threading;
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

        internal async Task WaitAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken
        )
        {
            using var timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );
            timeoutCancellation.CancelAfter(TimeoutMs);

            try
            {
                await operation(timeoutCancellation.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
                when (
                    !cancellationToken.IsCancellationRequested
                    && timeoutCancellation.IsCancellationRequested
                )
            {
                throw new TimeoutException(
                    $"{Operation} timed out after {TimeoutMs}ms",
                    ex
                );
            }
        }

        internal async Task<T> WaitAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken
        )
        {
            using var timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );
            timeoutCancellation.CancelAfter(TimeoutMs);

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
                throw new TimeoutException(
                    $"{Operation} timed out after {TimeoutMs}ms",
                    ex
                );
            }
        }

    }

    private static Task WaitForCloudOperationAsync(
        string operation,
        int timeoutMs,
        Func<CancellationToken, Task> run,
        CancellationToken cancellationToken
    )
        => new CloudOperationTimeout(operation, timeoutMs)
            .WaitAsync(run, cancellationToken);

    private static Task<T> WaitForCloudOperationAsync<T>(
        string operation,
        int timeoutMs,
        Func<CancellationToken, Task<T>> run,
        CancellationToken cancellationToken
    )
        => new CloudOperationTimeout(operation, timeoutMs)
            .WaitAsync(run, cancellationToken);

}
