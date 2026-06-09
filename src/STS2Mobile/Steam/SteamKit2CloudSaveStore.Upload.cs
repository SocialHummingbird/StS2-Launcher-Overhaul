using System;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudFileUpload
    {
        private CloudFileUpload(
            string path,
            byte[] rawBytes,
            ulong batchId,
            DateTimeOffset? timestamp,
            CloudUploadPayload payload,
            byte[] fileHash
        )
        {
            Path = path;
            RawBytes = rawBytes;
            BatchId = batchId;
            Timestamp = timestamp;
            Payload = payload;
            FileHash = fileHash;
        }

        internal string Path { get; }
        private byte[] RawBytes { get; }
        internal ulong BatchId { get; }
        private DateTimeOffset? Timestamp { get; }
        internal CloudUploadPayload Payload { get; }
        internal byte[] FileHash { get; }
        internal uint RawSize => (uint)RawBytes.Length;
        internal int RawByteCount => RawBytes.Length;
        internal int UploadSize => Payload.UploadSize();
        internal bool HasBatchId => BatchId != 0;
        internal ulong UploadTimestamp
            => (ulong)(Timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        internal static CloudFileUpload Create(
            string path,
            byte[] rawBytes,
            ulong batchId,
            DateTimeOffset? timestamp
        )
            => new(
                path,
                rawBytes,
                batchId,
                timestamp,
                CloudUploadPayload.FromRaw(rawBytes),
                ManagedSha1.Hash(rawBytes)
            );
    }

    private async Task UploadFileAsync(
        string canonPath,
        byte[] bytes,
        ulong batchId,
        DateTimeOffset? timestamp = null
    )
    {
        var upload = CloudFileUpload.Create(canonPath, bytes, batchId, timestamp);
        upload.Payload.LogUploadStart(upload.Path, upload.RawSize);

        var beginResult = await BeginFileUploadAsync(upload).ConfigureAwait(false);
        if (beginResult == null)
            return;

        await SendAndCommitUploadAsync(upload, beginResult).ConfigureAwait(false);
        upload.Payload.LogUploadComplete(upload.Path, upload.RawByteCount);
    }

    private async Task SendAndCommitUploadAsync(
        CloudFileUpload upload,
        CCloud_ClientBeginFileUpload_Response beginResult
    )
    {
        var uploadSucceeded = false;
        try
        {
            await upload.Payload
                .SendBlocksAsync(data => SendUploadBlocksAsync(beginResult, data))
                .ConfigureAwait(false);
            uploadSucceeded = true;
        }
        finally
        {
            await CommitFileUploadAsync(upload, uploadSucceeded)
                .ConfigureAwait(false);
        }
    }
}
