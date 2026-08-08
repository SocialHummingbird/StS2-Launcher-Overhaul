using System.Threading.Tasks;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private readonly struct ProductInfoFetchRequest
        {
            internal ProductInfoFetchRequest(ProductInfoApp app)
            {
                App = app;
            }

            private ProductInfoApp App { get; }

            internal async Task<PICSProductInfo?> FetchAsync()
                => await App._owner._connection.GetAppInfoAsync(
                    App.AppId,
                    await GetAccessTokenAsync()
                );

            private async Task<ulong> GetAccessTokenAsync()
            {
                var token = await App._owner._connection.GetAppAccessTokenOrPublicAsync(
                    App.AppId,
                    App.AccessTokenDenied()
                );
                if (token == 0)
                    App._owner.Log(App.PublicAccessTokenFallback());

                return token;
            }
        }

        private async Task<PICSProductInfo?> GetInfoAsync()
        {
            var request = new ProductInfoFetchRequest(this);
            return await _owner._productInfoAppCache.GetOrFetchAsync(
                AppId,
                request.FetchAsync
            );
        }
    }
}
