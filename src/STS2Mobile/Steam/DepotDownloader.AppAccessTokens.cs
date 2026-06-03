using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ProductInfoApp
    {
        private ProductInfoApp(uint appId)
        {
            AppId = appId;
        }

        private uint AppId { get; }
        private bool IsMainApp => AppId == SteamCloudApp.AppId;
        private string Name => IsMainApp
            ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
            : $"referenced app {AppId}";
        private string OwnershipHint => IsMainApp
            ? "; ownership/session may be invalid"
            : "";

        internal static Task<KeyValue> GetMainDepotsSectionAsync(DepotDownloader owner)
            => new ProductInfoApp(SteamCloudApp.AppId)
                .GetRequiredDepotsSectionAsync(owner);

        internal static Task<KeyValue> TryGetReferencedDepotsSectionAsync(
            DepotDownloader owner,
            uint appId
        )
            => new ProductInfoApp(appId)
                .TryGetDepotsSectionAsync(owner);

        private async Task<KeyValue> GetRequiredDepotsSectionAsync(DepotDownloader owner)
        {
            var appInfo = await GetRequiredInfoAsync(owner);
            return GetDepotsSection(appInfo);
        }

        private async Task<KeyValue> TryGetDepotsSectionAsync(DepotDownloader owner)
        {
            var appInfo = await GetInfoAsync(owner);
            return appInfo == null
                ? KeyValue.Invalid
                : GetDepotsSection(appInfo);
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo> GetRequiredInfoAsync(
            DepotDownloader owner
        )
        {
            var appInfo = await GetInfoAsync(owner);
            if (appInfo == null)
                throw new System.Exception(AppInfoUnavailable());

            return appInfo;
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetInfoAsync(
            DepotDownloader owner
        )
        {
            if (owner._appInfoCache.TryGetValue(AppId, out var cached))
                return cached;

            var appInfo = await FetchInfoAsync(owner);
            if (appInfo != null)
                owner._appInfoCache[AppId] = appInfo;

            return appInfo;
        }

        private async Task<ulong> GetAccessTokenAsync(DepotDownloader owner)
        {
            var token = await owner._connection.GetAppAccessTokenOrPublicAsync(
                AppId,
                AccessTokenDenied()
            );
            if (token == 0)
                owner.Log(PublicAccessTokenFallback());

            return token;
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> FetchInfoAsync(
            DepotDownloader owner
        )
            => await owner._connection.GetAppInfoAsync(
                AppId,
                await GetAccessTokenAsync(owner)
            );

        private KeyValue GetDepotsSection(
            SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo
        )
        {
            var depots = appInfo?.KeyValues?["depots"];
            if (depots == null || depots == KeyValue.Invalid)
                throw new System.InvalidOperationException(MissingDepotsSection());

            return depots;
        }

        private string AccessTokenDenied()
            => $"Steam denied app access token for {Name}{OwnershipHint}";

        private string PublicAccessTokenFallback()
            => $"Steam returned no app access token for {Name}; "
                + "continuing with public token 0";

        private string AppInfoUnavailable()
            => $"Failed to get app info from Steam for {Name}{OwnershipHint}";

        private string MissingDepotsSection()
            => $"Steam app info for {Name} has no depots section";
    }
}
