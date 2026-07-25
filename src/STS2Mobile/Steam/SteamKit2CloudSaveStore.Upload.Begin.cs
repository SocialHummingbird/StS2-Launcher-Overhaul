#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private async Task<CCloud_ClientBeginFileUpload_Response?> BeginFileUploadAsync(
        CloudFileUpload upload,
        CancellationToken cancellationToken
    )
    {
        var request = CreateBeginFileUploadRequest(upload);

        try
        {
            return await _connection
                .SendCloud<
                    CCloud_ClientBeginFileUpload_Request,
                    CCloud_ClientBeginFileUpload_Response
                >(
                    "ClientBeginFileUpload",
                    request,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DuplicateRequest"))
        {
            PatchHelper.Log(UploadSkippedAlreadyUpToDate(upload.Path));
            return null;
        }
    }
}
