using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;

    private void UploadWithRetry(
        string canonPath,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
        => RunCloudOperationWithRetry(
            "Upload",
            canonPath,
            attempt => (attempt + 1) * 2000,
            () =>
                UploadFileAsync(canonPath, bytes, batchId, timestamp)
                    .GetAwaiter()
                    .GetResult()
        );

    private void DeleteCloudFileWithRetry(string path)
        => RunCloudOperationWithRetry(
            "Delete",
            path,
            _ => 1000,
            () => DeleteCloudFile(path)
        );

    private static void RunCloudOperationWithRetry(
        string operationName,
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
                when (IsTooManyPending(ex) && HasCloudOperationRetryRemaining(attempt))
            {
                var delayMs = throttleDelayMs(attempt);
                PatchHelper.Log(OperationThrottled(operationName, path, delayMs));
                Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(OperationFailed(operationName, path, ex));
                return;
            }
        }
    }

    private static bool HasCloudOperationRetryRemaining(int attempt)
        => attempt < MaxCloudOperationAttempts - 1;

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
