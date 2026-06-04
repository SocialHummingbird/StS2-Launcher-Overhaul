using System.Threading.Tasks;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private async Task<PICSProductInfo?> GetInfoAsync()
            => await _owner._productInfoAppCache.GetOrFetchAsync(this);

        private async Task<ulong> GetAccessTokenAsync()
        {
            var token = await _owner._connection.GetAppAccessTokenOrPublicAsync(
                AppId,
                AccessTokenDenied()
            );
            if (token == 0)
                _owner.Log(PublicAccessTokenFallback());

            return token;
        }

        internal async Task<PICSProductInfo?> FetchInfoForCacheAsync()
            => await _owner._connection.GetAppInfoAsync(
                AppId,
                await GetAccessTokenAsync()
            );
    }
}
