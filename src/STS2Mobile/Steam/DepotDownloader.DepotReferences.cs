using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string PublicDepotBranch = "public";

    private async Task<ulong?> GetPublicManifestIdAsync(KeyValue depot, uint depotId)
        => await DepotManifestLookup.For(this, depot, depotId)
            .GetPublicManifestIdAsync();
}
