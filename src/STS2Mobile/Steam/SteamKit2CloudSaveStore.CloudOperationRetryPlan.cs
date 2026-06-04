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
        private readonly CloudStoreOperation _operation;
        private readonly string _path;
        private readonly Func<int, int> _throttleDelayMs;
        private readonly Action _run;

        private CloudOperationRetryPlan(
            CloudStoreOperation operation,
            string path,
            Func<int, int> throttleDelayMs,
            Action run
        )
        {
            _operation = operation;
            _path = path;
            _throttleDelayMs = throttleDelayMs;
            _run = run;
        }

        internal static CloudOperationRetryPlan Upload(string path, Action run)
            => new(
                UploadOperation,
                path,
                attempt => (attempt + 1) * UploadThrottleDelayStepMs,
                run
            );

        internal static CloudOperationRetryPlan Delete(string path, Action run)
            => new(
                DeleteOperation,
                path,
                _ => DeleteThrottleDelayMs,
                run
            );

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
                    when (CanRetryAfterThrottle(ex, attempt))
                {
                    WaitAfterThrottle(attempt);
                }
                catch (Exception ex)
                {
                    LogFailure(ex);
                    return;
                }
            }
        }

        private bool CanRetryAfterThrottle(InvalidOperationException ex, int attempt)
            => IsTooManyPending(ex) && HasRetryRemaining(attempt);

        private void WaitAfterThrottle(int attempt)
        {
            var delayMs = _throttleDelayMs(attempt);
            PatchHelper.Log(_operation.Throttled(_path, delayMs));
            Thread.Sleep(delayMs);
        }

        private void LogFailure(Exception ex)
            => PatchHelper.Log(_operation.FailedForPath(_path, ex));

        private static bool HasRetryRemaining(int attempt)
            => attempt < MaxCloudOperationAttempts - 1;

        private static bool IsTooManyPending(InvalidOperationException ex)
            => ex.Message.Contains("TooManyPending");
    }
}
