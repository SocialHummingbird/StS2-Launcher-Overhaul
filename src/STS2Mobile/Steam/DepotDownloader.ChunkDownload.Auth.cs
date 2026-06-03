using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private Task<CdnDownloadResult<int>> TryDownloadChunkWithAuthAsync(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName,
        CdnServerAttempt attempt
    )
        => RunCdnAuthRetryAsync<int>(
            depotId,
            attempt,
            ChunkAuthRetryOperation,
            async token =>
            {
                var written = await attempt.DownloadChunkAsync(
                    this,
                    depotId,
                    chunk,
                    buffer,
                    depotKey,
                    token
                );

                return ChunkHashVerifiedOrRetry(fileName, chunk, buffer, written, attempt)
                    ? CdnDownloadResult<int>.Success(written)
                    : CdnDownloadResult<int>.Retry();
            }
        );
}
