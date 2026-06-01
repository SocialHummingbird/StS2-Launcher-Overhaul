using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;

    private readonly struct CloudRetryOperation
    {
        internal CloudRetryOperation(
            string name,
            string path,
            Func<int, int> throttleDelayMs,
            Action run
        )
        {
            Name = name;
            Path = path;
            ThrottleDelayMs = throttleDelayMs;
            Run = run;
        }

        internal string Name { get; }
        internal string Path { get; }
        internal Func<int, int> ThrottleDelayMs { get; }
        internal Action Run { get; }
    }

    private void UploadWithRetry(
        string canonPath,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
        => RunCloudOperationWithRetry(
            new CloudRetryOperation(
                "Upload",
                canonPath,
                attempt => (attempt + 1) * 2000,
                () =>
                    UploadFileAsync(canonPath, bytes, batchId, timestamp)
                        .GetAwaiter()
                        .GetResult()
            )
        );

    private void DeleteCloudFileWithRetry(string path)
        => RunCloudOperationWithRetry(
            new CloudRetryOperation(
                "Delete",
                path,
                _ => 1000,
                () => DeleteCloudFile(path)
            )
        );

    private static void RunCloudOperationWithRetry(CloudRetryOperation operation)
    {
        for (var attempt = 0; attempt < MaxCloudOperationAttempts; attempt++)
        {
            try
            {
                operation.Run();
                return;
            }
            catch (InvalidOperationException ex)
                when (IsTooManyPending(ex) && HasCloudOperationRetryRemaining(attempt))
            {
                var delayMs = operation.ThrottleDelayMs(attempt);
                PatchHelper.Log(OperationThrottled(operation.Name, operation.Path, delayMs));
                Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(OperationFailed(operation.Name, operation.Path, ex));
                return;
            }
        }
    }

    private static bool HasCloudOperationRetryRemaining(int attempt)
        => attempt < MaxCloudOperationAttempts - 1;

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
