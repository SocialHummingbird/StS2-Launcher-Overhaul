using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
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

    private void DeleteCloudFileWithRetry(string path)
        => RunCloudOperationWithRetry(
            DeleteOperation,
            path,
            _ => DeleteThrottleDelayMs,
            () => DeleteCloudFile(path)
        );

    private static void RunCloudOperationWithRetry(
        CloudStoreOperation operation,
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
                PatchHelper.Log(operation.FailedForPath(path, ex));
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
        CloudStoreOperation operation,
        string path,
        int delayMs
    )
    {
        PatchHelper.Log(operation.Throttled(path, delayMs));
        Thread.Sleep(delayMs);
    }

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
