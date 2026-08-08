using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private async Task CommitFileUploadAsync(
        CloudFileUpload upload,
        bool uploadSucceeded,
        CancellationToken cancellationToken
    )
    {
        var commitResult = await _connection
            .SendCloud<
                CCloud_ClientCommitFileUpload_Request,
                CCloud_ClientCommitFileUpload_Response
            >(
                "ClientCommitFileUpload",
                CreateCommitFileUploadRequest(upload, uploadSucceeded),
                cancellationToken
            )
            .ConfigureAwait(false);

        RequireCommitted(
            upload.Path,
            uploadSucceeded,
            commitResult.file_committed
        );
    }

    internal static void RequireCommitted(
        string path,
        bool uploadSucceeded,
        bool fileCommitted
    )
    {
        if (uploadSucceeded && !fileCommitted)
            throw new InvalidOperationException(CommitReturnedFalse(path));
    }
}
