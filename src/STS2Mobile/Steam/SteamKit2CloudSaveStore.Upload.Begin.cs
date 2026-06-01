using System;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task<CCloud_ClientBeginFileUpload_Response?> BeginFileUploadAsync(
        CloudUploadMetadata upload
    )
    {
        var beginRequest = CreateBeginFileUploadRequest(upload);
        try
        {
            return await _connection
                .SendCloud<
                    CCloud_ClientBeginFileUpload_Request,
                    CCloud_ClientBeginFileUpload_Response
                >("ClientBeginFileUpload", beginRequest)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DuplicateRequest"))
        {
            PatchHelper.Log(UploadSkippedAlreadyUpToDate(upload.Path));
            return null;
        }
    }

    private static CCloud_ClientBeginFileUpload_Request CreateBeginFileUploadRequest(
        CloudUploadMetadata upload
    )
    {
        var uploadTimestamp = upload.Timestamp.HasValue
            ? (ulong)upload.Timestamp.Value.ToUnixTimeSeconds()
            : (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var request = new CCloud_ClientBeginFileUpload_Request
        {
            appid = SteamCloudApp.AppId,
            filename = upload.Path,
            file_size = (uint)upload.UploadSize,
            raw_file_size = upload.RawSize,
            file_sha = upload.FileHash,
            time_stamp = uploadTimestamp,
            can_encrypt = false,
            is_shared_file = false,
        };

        if (upload.BatchId != 0)
            request.upload_batch_id = upload.BatchId;

        return request;
    }
}
