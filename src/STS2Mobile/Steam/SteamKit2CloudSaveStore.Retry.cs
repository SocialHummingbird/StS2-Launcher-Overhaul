using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private const int MaxCloudOperationAttempts = 3;
    private const int UploadThrottleDelayStepMs = 2000;

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

    private static bool CanRetryAfterThrottle(
        InvalidOperationException ex,
        int attempt
    )
        => IsTooManyPending(ex) && attempt < MaxCloudOperationAttempts - 1;

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
