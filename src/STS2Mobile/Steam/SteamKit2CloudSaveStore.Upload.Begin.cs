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
}
