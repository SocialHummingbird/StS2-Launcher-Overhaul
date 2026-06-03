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
        var app = ProductInfoApp.Main();
        var appInfo = await app.GetRequiredInfoAsync(this);
        var depotSection = app.GetDepotsSection(appInfo);
        return await ParseDepotsAsync(depotSection);
    }

    private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetAppInfoAsync(
        ProductInfoApp app
    )
    {
        if (_appInfoCache.TryGetValue(app.AppId, out var cached))
            return cached;

        var appInfo = await app.FetchInfoAsync(this);
        if (appInfo != null)
            _appInfoCache[app.AppId] = appInfo;

        return appInfo;
    }
}
