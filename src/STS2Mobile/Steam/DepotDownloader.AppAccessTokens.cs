using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ProductInfoApp
    {
        private readonly DepotDownloader _owner;

        private ProductInfoApp(DepotDownloader owner, uint appId)
        {
            _owner = owner;
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
            => new ProductInfoApp(owner, SteamCloudApp.AppId)
                .GetRequiredDepotsSectionAsync();

        internal static Task<KeyValue> TryGetReferencedManifestSectionAsync(
            DepotDownloader owner,
            uint appId,
            uint depotId
        )
            => new ProductInfoApp(owner, appId)
                .TryGetManifestSectionAsync(depotId);

        private async Task<KeyValue> GetRequiredDepotsSectionAsync()
        {
            var appInfo = await GetRequiredInfoAsync();
            return GetDepotsSection(appInfo);
        }

        private async Task<KeyValue> TryGetManifestSectionAsync(uint depotId)
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                return KeyValue.Invalid;

            var depots = GetDepotsSection(appInfo);
            var depot = depots[depotId.ToString()];
            return depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo> GetRequiredInfoAsync()
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                throw new System.Exception(AppInfoUnavailable());

            return appInfo;
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetInfoAsync()
        {
            if (_owner._appInfoCache.TryGetValue(AppId, out var cached))
                return cached;

            var appInfo = await FetchInfoAsync();
            if (appInfo != null)
                _owner._appInfoCache[AppId] = appInfo;

            return appInfo;
        }

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

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> FetchInfoAsync()
            => await _owner._connection.GetAppInfoAsync(
                AppId,
                await GetAccessTokenAsync()
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
