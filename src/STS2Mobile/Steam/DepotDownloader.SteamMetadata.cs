using System;
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

    private async Task<List<(uint DepotId, ulong ManifestId)>> GetMainAppDepotsAsync()
    {
        var appInfo = await GetRequiredAppInfoAsync(SteamCloudApp.AppId);
        var depotSection = GetDepotsSection(appInfo, SteamCloudApp.AppId);
        return await ParseDepotsAsync(depotSection);
    }

    private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetAppInfoAsync(
        uint appId
    )
    {
        if (_appInfoCache.TryGetValue(appId, out var cached))
            return cached;

        var appInfo = await FetchAppInfoAsync(appId);
        if (appInfo != null)
            _appInfoCache[appId] = appInfo;

        return appInfo;
    }

    private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> FetchAppInfoAsync(
        uint appId
    )
    {
        return await _connection.GetAppInfoAsync(
            appId,
            await GetProductInfoAccessTokenAsync(appId)
        );
    }

    private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo> GetRequiredAppInfoAsync(
        uint appId
    )
    {
        var appInfo = await GetAppInfoAsync(appId);
        if (appInfo == null)
            throw new Exception("Failed to get app info from Steam");

        return appInfo;
    }

    private static KeyValue GetDepotsSection(
        SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo,
        uint appId
    )
    {
        var depots = appInfo?.KeyValues?["depots"];
        if (depots == null || depots == KeyValue.Invalid)
            throw new InvalidOperationException($"Steam app info for {appId} has no depots section");

        return depots;
    }
}
