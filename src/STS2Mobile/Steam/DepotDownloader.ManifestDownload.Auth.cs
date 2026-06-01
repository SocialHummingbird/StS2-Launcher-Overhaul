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
        CdnServerAttempt attempt
    )
    {
        return await RunCdnAuthRetryAsync<DepotManifest?>(
            depotId,
            attempt,
            "Manifest",
            token => attempt.DownloadManifestAsync(
                this,
                depotId,
                manifestId,
                manifestRequestCode,
                depotKey,
                token
            ),
            failed: null
        );
    }
}
