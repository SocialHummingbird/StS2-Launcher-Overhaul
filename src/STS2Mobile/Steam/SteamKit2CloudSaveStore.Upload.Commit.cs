using System;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task CommitFileUploadAsync(
        CloudUploadMetadata upload,
        bool uploadSucceeded
    )
    {
        try
        {
            var commitResult = await _connection
                .SendCloud<
                    CCloud_ClientCommitFileUpload_Request,
                    CCloud_ClientCommitFileUpload_Response
                >(
                    "ClientCommitFileUpload",
                    upload.CreateCommitRequest(uploadSucceeded)
                )
                .ConfigureAwait(false);

            if (uploadSucceeded && !commitResult.file_committed)
                upload.LogCommitReturnedFalse();
        }
        catch (Exception ex)
        {
            upload.LogCommitFailed(ex);
        }
    }
}
