using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private static CCloud_ClientBeginFileUpload_Request CreateBeginFileUploadRequest(
        CloudFileUpload upload
    )
    {
        var request = new CCloud_ClientBeginFileUpload_Request
        {
            appid = SteamCloudApp.AppId,
            filename = upload.Path,
            file_size = (uint)upload.UploadSize,
            raw_file_size = upload.RawSize,
            file_sha = upload.FileHash,
            time_stamp = upload.UploadTimestamp,
            can_encrypt = false,
            is_shared_file = false,
        };

        if (upload.HasBatchId)
            request.upload_batch_id = upload.BatchId;

        return request;
    }

    private static CCloud_ClientCommitFileUpload_Request CreateCommitFileUploadRequest(
        CloudFileUpload upload,
        bool uploadSucceeded
    )
        => new()
        {
            transfer_succeeded = uploadSucceeded,
            appid = SteamCloudApp.AppId,
            file_sha = upload.FileHash,
            filename = upload.Path,
        };
}
