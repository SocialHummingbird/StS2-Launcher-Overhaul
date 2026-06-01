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
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                UploadFileAsync(canonPath, bytes, batchId, timestamp)
                    .GetAwaiter()
                    .GetResult();
                return;
            }
            catch (InvalidOperationException ex)
                when (IsTooManyPending(ex) && attempt < 2)
            {
                var delayMs = (attempt + 1) * 2000;
                PatchHelper.Log(StoreMessage.OperationThrottled("Upload", canonPath, delayMs));
                Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(StoreMessage.OperationFailed("Upload", canonPath, ex));
                return;
            }
        }
    }

    private void DeleteCloudFileWithRetry(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                DeleteCloudFile(path);
                return;
            }
            catch (InvalidOperationException ex)
                when (IsTooManyPending(ex) && attempt < 2)
            {
                const int delayMs = 1000;
                PatchHelper.Log(StoreMessage.OperationThrottled("Delete", path, delayMs));
                Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(StoreMessage.OperationFailed("Delete", path, ex));
                return;
            }
        }
    }

    private static bool IsTooManyPending(InvalidOperationException ex)
        => ex.Message.Contains("TooManyPending");
}
