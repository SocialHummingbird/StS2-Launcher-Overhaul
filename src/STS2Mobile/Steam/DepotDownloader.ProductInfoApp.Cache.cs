using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ProductInfoAppCache
    {
        private readonly ConcurrentDictionary<uint, PICSProductInfo> _cache = new();

        internal async Task<PICSProductInfo?> GetOrFetchAsync(
            uint appId,
            Func<Task<PICSProductInfo?>> fetchAsync
        )
        {
            if (_cache.TryGetValue(appId, out var cached))
                return cached;

            var appInfo = await fetchAsync();
            if (appInfo != null)
                _cache[appId] = appInfo;

            return appInfo;
        }
    }
}
