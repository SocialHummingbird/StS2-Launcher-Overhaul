using System;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<DepotManifest> DownloadManifestWithRetriesAsync(
        uint depotId,
        ulong manifestId,
        ulong manifestRequestCode,
        byte[] depotKey
    )
    {
        Log($"Downloading manifest for depot {depotId}...");
        for (int attemptIndex = 0; attemptIndex < MaxRetries; attemptIndex++)
        {
            var attempt = (Server: GetCurrentServer(), Index: attemptIndex);
            try
            {
                return await _cdnClient.DownloadManifestAsync(
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    attempt.Server,
                    depotKey
                );
            }
            catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                var manifest = await TryDownloadManifestWithAuthAsync(
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    depotKey,
                    attempt
                );
                if (manifest != null)
                    return manifest;
            }
            catch (Exception ex) when (HasRetryRemaining(attempt))
            {
                HandleDownloadRetryFailure(attempt, "Manifest download", ex);
            }
        }

        throw new Exception(
            $"Failed to download manifest for depot {depotId} after {MaxRetries} attempts"
        );
    }
}
