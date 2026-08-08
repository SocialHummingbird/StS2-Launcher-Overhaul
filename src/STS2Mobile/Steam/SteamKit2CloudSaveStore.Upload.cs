using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
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
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var upload = CloudFileUpload.Create(canonPath, bytes, batchId, timestamp);
        cancellationToken.ThrowIfCancellationRequested();
        upload.Payload.LogUploadStart(upload.Path, upload.RawSize);

        var beginResult = await BeginFileUploadAsync(
            upload,
            cancellationToken
        ).ConfigureAwait(false);
        if (beginResult == null)
            return;

        await SendAndCommitUploadAsync(
            upload,
            beginResult,
            cancellationToken
        ).ConfigureAwait(false);
        upload.Payload.LogUploadComplete(upload.Path, upload.RawByteCount);
    }

    private async Task SendAndCommitUploadAsync(
        CloudFileUpload upload,
        CCloud_ClientBeginFileUpload_Response beginResult,
        CancellationToken cancellationToken
    )
    {
        var uploadSucceeded = false;
        Exception uploadFailure = null;
        try
        {
            await upload.Payload
                .SendBlocksAsync(
                    data => SendUploadBlocksAsync(
                        beginResult,
                        data,
                        cancellationToken
                    )
                )
                .ConfigureAwait(false);
            uploadSucceeded = true;
        }
        catch (Exception ex)
        {
            uploadFailure = ex;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            if (uploadFailure != null)
                ExceptionDispatchInfo.Capture(uploadFailure).Throw();

            cancellationToken.ThrowIfCancellationRequested();
        }

        try
        {
            await CommitFileUploadAsync(
                upload,
                uploadSucceeded,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception commitFailure) when (uploadFailure != null)
        {
            PatchHelper.Log(CommitFailed(upload.Path, commitFailure));
            ExceptionDispatchInfo.Capture(uploadFailure).Throw();
        }

        if (uploadFailure != null)
            ExceptionDispatchInfo.Capture(uploadFailure).Throw();
    }
}
