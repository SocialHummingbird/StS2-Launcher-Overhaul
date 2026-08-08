using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private async Task DeleteCloudFileAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _connection
                .SendCloud<
                    CCloud_ClientDeleteFile_Request,
                    CCloud_ClientDeleteFile_Response
                >(
                    "ClientDeleteFile",
                    CreateDeleteFileRequest(path),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (IsRemoteFileMissing(ex))
        {
            // Deleting an already absent file satisfies the tombstone.
        }
    }
}
