using System;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<DepotManifest?> TryDownloadManifestWithAuthAsync(
        uint depotId,
        ulong manifestId,
        ulong manifestRequestCode,
        byte[] depotKey,
        Server server,
        int attempt
    )
    {
        var token = await GetCdnAuthTokenForRetryAsync(depotId, server);
        if (token == null)
            return null;

        try
        {
            return await _cdnClient.DownloadManifestAsync(
                depotId,
                manifestId,
                manifestRequestCode,
                server,
                depotKey,
                cdnAuthToken: token
            );
        }
        catch (Exception tokenEx) when (attempt < MaxRetries - 1)
        {
            HandleCdnAuthRetryFailure(depotId, server, "Manifest", attempt, tokenEx);
            return null;
        }
    }
}
