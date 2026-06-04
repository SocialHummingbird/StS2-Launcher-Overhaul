using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;
    private const int DeleteThrottleDelayMs = 1000;
    private const int UploadThrottleDelayStepMs = 2000;

    private readonly struct CloudOperationRetryPlan
    {
        private readonly string _name;
        private readonly string _path;
        private readonly Func<int, int> _throttleDelayMs;
        private readonly Action _run;

        internal CloudOperationRetryPlan(
            string name,
            string path,
            Func<int, int> throttleDelayMs,
            Action run
        )
        {
            _name = name;
            _path = path;
            _throttleDelayMs = throttleDelayMs;
            _run = run;
        }

        internal void Run()
        {
            for (var attempt = 0; attempt < MaxCloudOperationAttempts; attempt++)
            {
                try
                {
                    _run();
                    return;
                }
                catch (InvalidOperationException ex)
                    when (IsTooManyPending(ex) && HasCloudOperationRetryRemaining(attempt))
                {
                    var delayMs = _throttleDelayMs(attempt);
                    PatchHelper.Log(OperationThrottled(_name, _path, delayMs));
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(OperationFailed(_name, _path, ex));
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
        => new CloudOperationRetryPlan(
            "Upload",
            canonPath,
            attempt => (attempt + 1) * UploadThrottleDelayStepMs,
            () => UploadFileAsync(canonPath, bytes, batchId, timestamp)
                .GetAwaiter()
                .GetResult()
        ).Run();

    private void DeleteCloudFileWithRetry(string path)
        => new CloudOperationRetryPlan(
            "Delete",
            path,
            _ => DeleteThrottleDelayMs,
            () => DeleteCloudFile(path)
        ).Run();

    private static bool HasCloudOperationRetryRemaining(int attempt)
        => attempt < MaxCloudOperationAttempts - 1;

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
