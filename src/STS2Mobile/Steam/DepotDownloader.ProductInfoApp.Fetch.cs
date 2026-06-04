using System.Threading.Tasks;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private async Task<PICSProductInfo?> GetInfoAsync()
        {
            if (_owner._appInfoCache.TryGetValue(Identity.AppId, out var cached))
                return cached;

            var appInfo = await FetchInfoAsync();
            if (appInfo != null)
                _owner._appInfoCache[Identity.AppId] = appInfo;

            return appInfo;
        }

        private async Task<ulong> GetAccessTokenAsync()
        {
            var token = await _owner._connection.GetAppAccessTokenOrPublicAsync(
                Identity.AppId,
                Identity.AccessTokenDenied()
            );
            if (token == 0)
                _owner.Log(Identity.PublicAccessTokenFallback());

            return token;
        }

        private async Task<PICSProductInfo?> FetchInfoAsync()
            => await _owner._connection.GetAppInfoAsync(
                Identity.AppId,
                await GetAccessTokenAsync()
            );
    }
}
