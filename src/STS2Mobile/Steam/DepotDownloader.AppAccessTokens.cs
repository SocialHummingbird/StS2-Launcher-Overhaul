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
            Identity = ProductInfoAppIdentity.For(appId);
        }

        private ProductInfoAppIdentity Identity { get; }
        internal uint AppId => Identity.AppId;

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
            return Sections(appInfo).RequiredDepots();
        }

        private async Task<KeyValue> TryGetManifestSectionAsync(uint depotId)
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                return KeyValue.Invalid;

            return Sections(appInfo).TryManifestSection(depotId);
        }

        private async Task<PICSProductInfo> GetRequiredInfoAsync()
        {
            var appInfo = await GetInfoAsync();
            if (appInfo == null)
                throw new System.Exception(Identity.AppInfoUnavailable());

            return appInfo;
        }

        private ProductInfoAppSections Sections(PICSProductInfo appInfo)
            => ProductInfoAppSections.For(Identity, appInfo);
    }
}
