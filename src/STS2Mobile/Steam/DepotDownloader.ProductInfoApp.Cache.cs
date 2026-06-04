using System.Collections.Concurrent;
using System.Threading.Tasks;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ProductInfoAppCache
    {
        private readonly ConcurrentDictionary<uint, PICSProductInfo> _cache = new();

        internal async Task<PICSProductInfo?> GetOrFetchAsync(ProductInfoApp app)
        {
            if (_cache.TryGetValue(app.AppId, out var cached))
                return cached;

            var appInfo = await app.FetchInfoForCacheAsync();
            if (appInfo != null)
                _cache[app.AppId] = appInfo;

            return appInfo;
        }
    }
}
