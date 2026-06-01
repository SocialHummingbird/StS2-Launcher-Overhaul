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
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var server = GetCurrentServer();
            try
            {
                return await _cdnClient.DownloadManifestAsync(
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    server,
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
                    server,
                    attempt
                );
                if (manifest != null)
                    return manifest;
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                HandleDownloadRetryFailure(server, "Manifest download", attempt, ex);
            }
        }

        throw new Exception(
            $"Failed to download manifest for depot {depotId} after {MaxRetries} attempts"
        );
    }
}
