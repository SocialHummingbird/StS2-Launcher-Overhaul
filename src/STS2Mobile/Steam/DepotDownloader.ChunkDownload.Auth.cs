using System;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<int?> TryDownloadChunkWithAuthAsync(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName,
        Server server,
        int attempt
    )
    {
        var token = await GetCdnAuthTokenForRetryAsync(depotId, server);
        if (token == null)
            return null;

        try
        {
            var written = await _cdnClient.DownloadDepotChunkAsync(
                depotId,
                chunk,
                server,
                buffer,
                depotKey,
                cdnAuthToken: token
            );

            return ChunkHashVerifiedOrRetry(fileName, chunk, buffer, written, attempt)
                ? written
                : null;
        }
        catch (Exception tokenEx) when (attempt < MaxRetries - 1)
        {
            HandleCdnAuthRetryFailure(depotId, server, "Chunk", attempt, tokenEx);
            return null;
        }
    }
}
