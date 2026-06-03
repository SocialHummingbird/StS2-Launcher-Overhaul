using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly ConcurrentDictionary<
        uint,
        SteamApps.PICSProductInfoCallback.PICSProductInfo
    > _appInfoCache = new();

    private async Task<List<DepotManifestReference>> GetMainAppDepotsAsync()
    {
        var depotSection = await ProductInfoApp.GetMainDepotsSectionAsync(this);
        return await ParseDepotsAsync(depotSection);
    }
}
