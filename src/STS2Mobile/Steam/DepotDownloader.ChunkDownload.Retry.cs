using System;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private Task<int> DownloadChunkWithRetriesAsync(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName
    )
        => RunCdnDownloadWithRetriesAsync(
            CreateChunkDownloadOperation(
                depotId,
                chunk,
                buffer,
                depotKey,
                fileName
            )
        );

    private CdnDownloadOperation<int> CreateChunkDownloadOperation(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName
    )
        => CdnDownloadOperation<int>.AcrossServersWithAuthRetry(
            ChunkDownloadOperation,
            attempt => TryDownloadChunkAsync(
                depotId,
                chunk,
                buffer,
                depotKey,
                fileName,
                attempt
            ),
            attempt => TryDownloadChunkWithAuthAsync(
                depotId,
                chunk,
                buffer,
                depotKey,
                fileName,
                attempt
            ),
            () => new Exception(
                $"Failed to download chunk for {fileName} after {MaxRetries} attempts"
            )
        );

    private Task<CdnDownloadResult<int>> TryDownloadChunkAsync(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName,
        CdnServerAttempt attempt
    )
        => CdnDownloadResult<int>.FromValidatedAsync(
            () => attempt.DownloadChunkAsync(
                this,
                depotId,
                chunk,
                buffer,
                depotKey
            ),
            written => ChunkHashVerifiedOrRetry(
                fileName,
                chunk,
                buffer,
                written,
                attempt
            )
        );

    private bool ChunkHashVerifiedOrRetry(
        string fileName,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        int written,
        CdnServerAttempt attempt
    )
    {
        if (VerifyChunkHash(buffer, written, chunk))
            return true;

        if (attempt.CanRetry())
        {
            Log($"Chunk SHA-1 mismatch at offset {chunk.Offset}, retrying...");
            return false;
        }

        throw new Exception(
            $"Chunk SHA-1 verification failed for {fileName} "
                + $"at offset {chunk.Offset} after {MaxRetries} attempts"
        );
    }
}
