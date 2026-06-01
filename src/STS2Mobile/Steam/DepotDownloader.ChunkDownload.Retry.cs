using System;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<int> DownloadChunkWithRetriesAsync(
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        byte[] depotKey,
        string fileName
    )
    {
        foreach (var attempt in CdnDownloadAttempts())
        {
            try
            {
                var written = await _cdnClient.DownloadDepotChunkAsync(
                    depotId,
                    chunk,
                    attempt.Server,
                    buffer,
                    depotKey
                );

                if (ChunkHashVerifiedOrRetry(fileName, chunk, buffer, written, attempt))
                    return written;
            }
            catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                var written = await TryDownloadChunkWithAuthAsync(
                    depotId,
                    chunk,
                    buffer,
                    depotKey,
                    fileName,
                    attempt
                );
                if (written.HasValue)
                    return written.Value;
            }
            catch (Exception ex) when (attempt.HasRetryRemaining)
            {
                HandleDownloadRetryFailure(attempt, "Chunk download", ex);
            }
        }

        throw new Exception($"Failed to download chunk for {fileName} after {MaxRetries} attempts");
    }

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

        if (attempt.HasRetryRemaining)
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
