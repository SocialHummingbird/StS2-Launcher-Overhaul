using System;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private static CCloud_ClientBeginFileUpload_Request CreateBeginFileUploadRequest(
        string path,
        int uploadSize,
        uint rawSize,
        byte[] fileHash,
        ulong batchId,
        DateTimeOffset? timestamp
    )
    {
        var uploadTimestamp = timestamp.HasValue
            ? (ulong)timestamp.Value.ToUnixTimeSeconds()
            : (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var request = new CCloud_ClientBeginFileUpload_Request
        {
            appid = SteamCloudApp.AppId,
            filename = path,
            file_size = (uint)uploadSize,
            raw_file_size = rawSize,
            file_sha = fileHash,
            time_stamp = uploadTimestamp,
            can_encrypt = false,
            is_shared_file = false,
        };

        if (batchId != 0)
            request.upload_batch_id = batchId;

        return request;
    }

    private static CCloud_ClientCommitFileUpload_Request CreateCommitFileUploadRequest(
        string path,
        byte[] fileHash,
        bool uploadSucceeded
    )
        => new()
        {
            transfer_succeeded = uploadSucceeded,
            appid = SteamCloudApp.AppId,
            file_sha = fileHash,
            filename = path,
        };
}
