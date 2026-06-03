using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task UploadFileAsync(
        string canonPath,
        byte[] bytes,
        ulong batchId,
        DateTimeOffset? timestamp = null
    )
    {
        var upload = CloudUploadPayload.FromRaw(bytes);
        var fileHash = SHA1.HashData(bytes);
        upload.LogUploadStart(canonPath, (uint)bytes.Length);

        var beginResult = await BeginFileUploadAsync(
            canonPath,
            upload.UploadSize(),
            (uint)bytes.Length,
            fileHash,
            batchId,
            timestamp
        ).ConfigureAwait(false);
        if (beginResult == null)
            return;

        bool uploadSucceeded = false;
        try
        {
            await upload
                .SendBlocksAsync(data => SendUploadBlocksAsync(beginResult, data))
                .ConfigureAwait(false);
            uploadSucceeded = true;
        }
        finally
        {
            await CommitFileUploadAsync(canonPath, fileHash, uploadSucceeded)
                .ConfigureAwait(false);
        }

        if (!uploadSucceeded)
            throw new InvalidOperationException($"Cloud upload failed for {canonPath}");

        upload.LogUploadComplete(canonPath, bytes.Length);
    }
}
