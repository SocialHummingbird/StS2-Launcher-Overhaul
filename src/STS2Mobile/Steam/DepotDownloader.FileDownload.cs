using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task DownloadFileAsync(
        DepotManifest.FileData file,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var fileName = GetManifestFileName(file);
        if (fileName == null)
            return;

        var target = DepotFileTarget.Create(this, fileName);
        await target.DownloadAsync(this, file, depotId, depotKey, ct);
    }
}
