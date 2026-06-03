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
        foreach (var attempt in CdnDownloadAttempts())
        {
            try
            {
                return await attempt.DownloadManifestAsync(
                    this,
                    depotId,
                    manifestId,
                    manifestRequestCode,
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
            catch (Exception ex) when (attempt.CanRetry())
            {
                attempt.HandleDownloadRetryFailure(this, ManifestDownloadOperation, ex);
            }
        }

        throw new Exception(
            $"Failed to download manifest for depot {depotId} after {MaxRetries} attempts"
        );
    }
}
