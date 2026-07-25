using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;
    private const int DeleteThrottleDelayMs = 1000;
    private const int UploadThrottleDelayStepMs = 2000;

    private void UploadWithRetry(
        string canonPath,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
        => RunCloudOperationWithRetry(
            UploadOperation,
            canonPath,
            attempt => (attempt + 1) * UploadThrottleDelayStepMs,
            () => UploadFileAsync(canonPath, bytes, batchId, timestamp)
                .GetAwaiter()
                .GetResult()
        );

    private async Task UploadWithRetryAsync(
        string canonPath,
        byte[] bytes,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        for (
            var attempt = 0;
            attempt < MaxCloudOperationAttempts;
            attempt++
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await UploadFileAsync(
                    canonPath,
                    bytes,
                    batchId: 0,
                    timestamp,
                    cancellationToken
                ).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (InvalidOperationException ex)
                when (CanRetryAfterThrottle(ex, attempt))
            {
                var delayMs =
                    (attempt + 1) * UploadThrottleDelayStepMs;
                PatchHelper.Log(
                    OperationThrottled(
                        UploadOperation,
                        canonPath,
                        delayMs
                    )
                );
                await Task.Delay(
                    delayMs,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    OperationFailedForPath(
                        UploadOperation,
                        canonPath,
                        ex
                    )
                );
                throw;
            }
        }
    }

    private void DeleteCloudFileWithRetry(string path)
        => RunCloudOperationWithRetry(
            DeleteOperation,
            path,
            _ => DeleteThrottleDelayMs,
            () => DeleteCloudFile(path)
        );

    private static void RunCloudOperationWithRetry(
        string operation,
        string path,
        Func<int, int> throttleDelayMs,
        Action run
    )
    {
        for (var attempt = 0; attempt < MaxCloudOperationAttempts; attempt++)
        {
            try
            {
                run();
                return;
            }
            catch (InvalidOperationException ex)
                when (CanRetryAfterThrottle(ex, attempt))
            {
                WaitAfterThrottle(operation, path, throttleDelayMs(attempt));
            }
            catch (Exception ex)
            {
                PatchHelper.Log(OperationFailedForPath(operation, path, ex));
                return;
            }
        }
    }

    private static bool CanRetryAfterThrottle(
        InvalidOperationException ex,
        int attempt
    )
        => IsTooManyPending(ex) && attempt < MaxCloudOperationAttempts - 1;

    private static void WaitAfterThrottle(
        string operation,
        string path,
        int delayMs
    )
    {
        PatchHelper.Log(OperationThrottled(operation, path, delayMs));
        Thread.Sleep(delayMs);
    }

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
