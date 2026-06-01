using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudUploadMetadata
    {
        private CloudUploadMetadata(
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

        private string Path { get; }
        private int UploadSize { get; }
        private uint RawSize { get; }
        private byte[] FileHash { get; }
        private ulong BatchId { get; }
        private DateTimeOffset? Timestamp { get; }

        internal static CloudUploadMetadata Create(
            string path,
            byte[] raw,
            CloudUploadPayload upload,
            ulong batchId,
            DateTimeOffset? timestamp
        )
            => new(
                path,
                upload.UploadSize(),
                (uint)raw.Length,
                SHA1.HashData(raw),
                batchId,
                timestamp
            );

        internal CCloud_ClientBeginFileUpload_Request CreateBeginRequest()
        {
            var uploadTimestamp = Timestamp.HasValue
                ? (ulong)Timestamp.Value.ToUnixTimeSeconds()
                : (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var request = new CCloud_ClientBeginFileUpload_Request
            {
                appid = SteamCloudApp.AppId,
                filename = Path,
                file_size = (uint)UploadSize,
                raw_file_size = RawSize,
                file_sha = FileHash,
                time_stamp = uploadTimestamp,
                can_encrypt = false,
                is_shared_file = false,
            };

            if (BatchId != 0)
                request.upload_batch_id = BatchId;

            return request;
        }

        internal CCloud_ClientCommitFileUpload_Request CreateCommitRequest(
            bool uploadSucceeded
        )
            => new()
            {
                transfer_succeeded = uploadSucceeded,
                appid = SteamCloudApp.AppId,
                file_sha = FileHash,
                filename = Path,
            };

        internal InvalidOperationException CreateFailedException()
            => new($"Cloud upload failed for {Path}");

        internal void LogCommitFailed(Exception ex)
            => PatchHelper.Log(CommitFailed(Path, ex));

        internal void LogCommitReturnedFalse()
            => PatchHelper.Log(CommitReturnedFalse(Path));

        internal void LogSkippedAlreadyUpToDate()
            => PatchHelper.Log(UploadSkippedAlreadyUpToDate(Path));

        internal void LogUploadComplete(int rawBytes, CloudUploadPayload upload)
            => upload.LogUploadComplete(Path, rawBytes);

        internal void LogUploadStart(CloudUploadPayload upload)
            => upload.LogUploadStart(Path, RawSize);
    }

    private async Task UploadFileAsync(
        string canonPath,
        byte[] bytes,
        ulong batchId,
        DateTimeOffset? timestamp = null
    )
    {
        var upload = CompressCloudFile(bytes);
        var metadata = CloudUploadMetadata.Create(
            canonPath,
            bytes,
            upload,
            batchId,
            timestamp
        );

        metadata.LogUploadStart(upload);

        var beginResult = await BeginFileUploadAsync(metadata).ConfigureAwait(false);
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
            await CommitFileUploadAsync(metadata, uploadSucceeded).ConfigureAwait(false);
        }

        if (!uploadSucceeded)
            throw metadata.CreateFailedException();

        metadata.LogUploadComplete(bytes.Length, upload);
    }
}
