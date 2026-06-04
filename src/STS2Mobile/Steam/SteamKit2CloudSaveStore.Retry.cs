using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private void UploadWithRetry(
        string canonPath,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
        => CloudOperationRetryPlan.Upload(
            canonPath,
            () => UploadFileAsync(canonPath, bytes, batchId, timestamp)
                .GetAwaiter()
                .GetResult()
        ).Run();

    private void DeleteCloudFileWithRetry(string path)
        => CloudOperationRetryPlan.Delete(
            path,
            () => DeleteCloudFile(path)
        ).Run();
}
