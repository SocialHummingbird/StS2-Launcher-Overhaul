using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;
    private const int DeleteThrottleDelayMs = 1000;
    private const int UploadThrottleDelayStepMs = 2000;

    private readonly struct CloudRetryOperation
    {
        private CloudRetryOperation(
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

        private string Name { get; }
        private string Path { get; }
        private Func<int, int> ThrottleDelayMs { get; }
        private Action Run { get; }

        internal static CloudRetryOperation Upload(string path, Action run)
            => new(
                "Upload",
                path,
                attempt => (attempt + 1) * UploadThrottleDelayStepMs,
                run
            );

        internal static CloudRetryOperation Delete(string path, Action run)
            => new(
                "Delete",
                path,
                _ => DeleteThrottleDelayMs,
                run
            );

        internal void RunWithRetry()
        {
            for (var attempt = 0; attempt < MaxCloudOperationAttempts; attempt++)
            {
                try
                {
                    Run();
                    return;
                }
                catch (InvalidOperationException ex)
                    when (IsTooManyPending(ex) && HasCloudOperationRetryRemaining(attempt))
                {
                    var delayMs = ThrottleDelayMs(attempt);
                    PatchHelper.Log(OperationThrottled(Name, Path, delayMs));
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(OperationFailed(Name, Path, ex));
                    return;
                }
            }
        }
    }

    private void UploadWithRetry(
        string canonPath,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
        => RunCloudOperationWithRetry(
            CloudRetryOperation.Upload(
                canonPath,
                () => UploadFileAsync(canonPath, bytes, batchId, timestamp)
                    .GetAwaiter()
                    .GetResult()
            )
        );

    private void DeleteCloudFileWithRetry(string path)
        => RunCloudOperationWithRetry(
            CloudRetryOperation.Delete(path, () => DeleteCloudFile(path))
        );

    private static void RunCloudOperationWithRetry(CloudRetryOperation operation)
        => operation.RunWithRetry();

    private static bool HasCloudOperationRetryRemaining(int attempt)
        => attempt < MaxCloudOperationAttempts - 1;

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
