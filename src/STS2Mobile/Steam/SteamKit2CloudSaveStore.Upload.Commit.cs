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
                    new CCloud_ClientCommitFileUpload_Request
                    {
                        transfer_succeeded = uploadSucceeded,
                        appid = SteamCloudApp.AppId,
                        file_sha = upload.FileHash,
                        filename = upload.Path,
                    }
                )
                .ConfigureAwait(false);

            if (uploadSucceeded && !commitResult.file_committed)
                PatchHelper.Log(CommitReturnedFalse(upload.Path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CommitFailed(upload.Path, ex));
        }
    }
}
