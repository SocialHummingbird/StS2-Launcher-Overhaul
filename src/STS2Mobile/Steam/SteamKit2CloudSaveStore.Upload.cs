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
        var fileHash = SHA1.HashData(bytes);
        var rawSize = (uint)bytes.Length;
        var (uploadBytes, compressed) = CompressCloudFile(bytes);

        if (compressed)
            PatchHelper.Log(StoreMessage.Compressed(canonPath, rawSize, uploadBytes.Length));
        else
            PatchHelper.Log(StoreMessage.UploadingUncompressed(canonPath, rawSize));

        var beginResult = await BeginFileUploadAsync(
            canonPath,
            uploadBytes.Length,
            rawSize,
            fileHash,
            batchId,
            timestamp
        ).ConfigureAwait(false);
        if (beginResult == null)
            return;

        bool uploadSucceeded = false;
        try
        {
            await SendUploadBlocksAsync(beginResult, uploadBytes).ConfigureAwait(false);
            uploadSucceeded = true;
        }
        finally
        {
            await CommitFileUploadAsync(canonPath, fileHash, uploadSucceeded).ConfigureAwait(false);
        }

        if (!uploadSucceeded)
            throw new InvalidOperationException($"Cloud upload failed for {canonPath}");

        PatchHelper.Log(StoreMessage.Wrote(canonPath, bytes.Length, compressed));
    }
}
