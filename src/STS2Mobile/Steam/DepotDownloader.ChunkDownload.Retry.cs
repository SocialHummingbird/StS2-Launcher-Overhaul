using System;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;

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
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var server = GetCurrentServer();
            try
            {
                var written = await _cdnClient.DownloadDepotChunkAsync(
                    depotId,
                    chunk,
                    server,
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
                    server,
                    attempt
                );
                if (written.HasValue)
                    return written.Value;
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                HandleDownloadRetryFailure(server, "Chunk download", attempt, ex);
            }
        }

        throw new Exception($"Failed to download chunk for {fileName} after {MaxRetries} attempts");
    }

    private bool ChunkHashVerifiedOrRetry(
        string fileName,
        DepotManifest.ChunkData chunk,
        byte[] buffer,
        int written,
        int attempt
    )
    {
        if (VerifyChunkHash(buffer, written, chunk))
            return true;

        if (attempt < MaxRetries - 1)
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
