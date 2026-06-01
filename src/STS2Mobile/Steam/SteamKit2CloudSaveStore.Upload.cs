using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudUploadMetadata
    {
        internal CloudUploadMetadata(
            string path,
            int uploadSize,
            uint rawSize,
            byte[] fileHash,
            ulong batchId,
            DateTimeOffset? timestamp
        )
        {
            Path = path;
            UploadSize = uploadSize;
            RawSize = rawSize;
            FileHash = fileHash;
            BatchId = batchId;
            Timestamp = timestamp;
        }

        internal string Path { get; }
        internal int UploadSize { get; }
        internal uint RawSize { get; }
        internal byte[] FileHash { get; }
        internal ulong BatchId { get; }
        internal DateTimeOffset? Timestamp { get; }
    }

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
        var metadata = new CloudUploadMetadata(
            canonPath,
            upload.UploadSize,
            rawSize,
            fileHash,
            batchId,
            timestamp
        );

        upload.LogUploadStart(metadata.Path, metadata.RawSize);

        var beginResult = await BeginFileUploadAsync(metadata).ConfigureAwait(false);
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
            await CommitFileUploadAsync(metadata, uploadSucceeded).ConfigureAwait(false);
        }

        if (!uploadSucceeded)
            throw new InvalidOperationException($"Cloud upload failed for {metadata.Path}");

        PatchHelper.Log(Wrote(metadata.Path, bytes.Length, upload.Compressed));
    }
}
