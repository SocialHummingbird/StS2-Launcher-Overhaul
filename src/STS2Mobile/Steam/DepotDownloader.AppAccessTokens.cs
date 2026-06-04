using System.Threading.Tasks;
using SteamKit2;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private readonly DepotDownloader _owner;

        private ProductInfoApp(DepotDownloader owner, uint appId)
        {
            _owner = owner;
            AppId = appId;
        }

        internal uint AppId { get; }

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
            return RequiredDepotsSection(appInfo);
        }

        private async Task<KeyValue> TryGetManifestSectionAsync(uint depotId)
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                return KeyValue.Invalid;

            return TryManifestSection(appInfo, depotId);
        }

        private async Task<PICSProductInfo> GetRequiredInfoAsync()
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                throw new System.Exception(AppInfoUnavailable());

            return appInfo;
        }

        private KeyValue RequiredDepotsSection(PICSProductInfo appInfo)
        {
            var depots = appInfo?.KeyValues?["depots"];
            if (depots == null || depots == KeyValue.Invalid)
                throw new System.InvalidOperationException(
                    MissingDepotsSection()
                );

            return depots;
        }

        private KeyValue TryManifestSection(PICSProductInfo appInfo, uint depotId)
        {
            var depot = RequiredDepotsSection(appInfo)[depotId.ToString()];
            return depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
        }
    }
}
