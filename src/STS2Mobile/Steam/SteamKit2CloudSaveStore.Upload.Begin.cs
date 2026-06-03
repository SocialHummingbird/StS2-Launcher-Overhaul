using System;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task<CCloud_ClientBeginFileUpload_Response?> BeginFileUploadAsync(
        string path,
        int uploadSize,
        uint rawSize,
        byte[] fileHash,
        ulong batchId,
        DateTimeOffset? timestamp
    )
    {
        var request = CreateBeginFileUploadRequest(
            path,
            uploadSize,
            rawSize,
            fileHash,
            batchId,
            timestamp
        );

        try
        {
            return await _connection
                .SendCloud<
                    CCloud_ClientBeginFileUpload_Request,
                    CCloud_ClientBeginFileUpload_Response
                >("ClientBeginFileUpload", request)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DuplicateRequest"))
        {
            PatchHelper.Log(UploadSkippedAlreadyUpToDate(path));
            return null;
        }
    }

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
}
