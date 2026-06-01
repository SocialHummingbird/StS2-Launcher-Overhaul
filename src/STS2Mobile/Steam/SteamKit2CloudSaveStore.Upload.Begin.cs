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
        try
        {
            return await _connection
                .SendCloud<
                    CCloud_ClientBeginFileUpload_Request,
                    CCloud_ClientBeginFileUpload_Response
                >("ClientBeginFileUpload", upload.CreateBeginRequest())
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DuplicateRequest"))
        {
            upload.LogSkippedAlreadyUpToDate();
            return null;
        }
    }
}
