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
        var upload = CompressCloudFile(bytes);

        if (upload.Compressed)
            PatchHelper.Log(Compressed(canonPath, rawSize, upload.Data.Length));
        else
            PatchHelper.Log(UploadingUncompressed(canonPath, rawSize));

        var beginResult = await BeginFileUploadAsync(
            canonPath,
            upload.Data.Length,
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
            await SendUploadBlocksAsync(beginResult, upload.Data).ConfigureAwait(false);
            uploadSucceeded = true;
        }
        finally
        {
            await CommitFileUploadAsync(canonPath, fileHash, uploadSucceeded).ConfigureAwait(false);
        }

        if (!uploadSucceeded)
            throw new InvalidOperationException($"Cloud upload failed for {canonPath}");

        PatchHelper.Log(Wrote(canonPath, bytes.Length, upload.Compressed));
    }
}
