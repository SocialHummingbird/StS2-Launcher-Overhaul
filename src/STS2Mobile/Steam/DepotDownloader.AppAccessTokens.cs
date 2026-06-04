using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ProductInfoApp
    {
        private readonly struct ProductInfoAppIdentity
        {
            internal ProductInfoAppIdentity(uint appId)
            {
                AppId = appId;
            }

            internal uint AppId { get; }

            private bool IsMainApp => AppId == SteamCloudApp.AppId;
            private string Name => IsMainApp
                ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
                : $"referenced app {AppId}";
            private string OwnershipHint => IsMainApp
                ? "; ownership/session may be invalid"
                : "";

            internal string AccessTokenDenied()
                => $"Steam denied app access token for {Name}{OwnershipHint}";

            internal string PublicAccessTokenFallback()
                => $"Steam returned no app access token for {Name}; "
                    + "continuing with public token 0";

            internal string AppInfoUnavailable()
                => $"Failed to get app info from Steam for {Name}{OwnershipHint}";

            internal string MissingDepotsSection()
                => $"Steam app info for {Name} has no depots section";
        }

        private readonly DepotDownloader _owner;

        private ProductInfoApp(DepotDownloader owner, uint appId)
        {
            _owner = owner;
            Identity = new ProductInfoAppIdentity(appId);
        }

        private ProductInfoAppIdentity Identity { get; }

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
                throw new System.Exception(Identity.AppInfoUnavailable());

            return appInfo;
        }

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetInfoAsync()
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

        private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> FetchInfoAsync()
            => await _owner._connection.GetAppInfoAsync(
                Identity.AppId,
                await GetAccessTokenAsync()
            );

        private KeyValue GetDepotsSection(
            SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo
        )
        {
            var depots = appInfo?.KeyValues?["depots"];
            if (depots == null || depots == KeyValue.Invalid)
                throw new System.InvalidOperationException(Identity.MissingDepotsSection());

            return depots;
        }
    }
}
