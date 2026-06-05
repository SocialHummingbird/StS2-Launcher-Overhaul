using System.Threading.Tasks;
using SteamKit2;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private readonly DepotDownloader _owner;

        private readonly struct ProductInfoDepots
        {
            private ProductInfoDepots(KeyValue depots)
            {
                Depots = depots;
            }

            private KeyValue Depots { get; }

            internal static KeyValue RequiredSection(
                ProductInfoApp app,
                PICSProductInfo? appInfo
            )
                => Required(app, appInfo).Depots;

            internal static KeyValue TryGetManifestSection(
                ProductInfoApp app,
                PICSProductInfo? appInfo,
                uint depotId
            )
            {
                if (appInfo == null)
                    return KeyValue.Invalid;

                return Required(app, appInfo).ManifestSection(depotId);
            }

            private static ProductInfoDepots Required(
                ProductInfoApp app,
                PICSProductInfo? appInfo
            )
            {
                if (appInfo == null)
                    throw new System.Exception(app.AppInfoUnavailable());

                var depots = appInfo.KeyValues?["depots"];
                if (depots == null || depots == KeyValue.Invalid)
                    throw new System.InvalidOperationException(
                        app.MissingDepotsSection()
                    );

                return new ProductInfoDepots(depots);
            }

            private KeyValue ManifestSection(uint depotId)
            {
                var depot = Depots[depotId.ToString()];
                return depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
            }
        }

        private ProductInfoApp(DepotDownloader owner, uint appId)
        {
            _owner = owner;
            AppId = appId;
        }

        private uint AppId { get; }

        private static ProductInfoApp Main(DepotDownloader owner)
            => new(owner, SteamCloudApp.AppId);

        private static ProductInfoApp Referenced(DepotDownloader owner, uint appId)
            => new(owner, appId);

        internal static Task<KeyValue> GetMainDepotsSectionAsync(DepotDownloader owner)
            => Main(owner).GetRequiredDepotsSectionAsync();

        internal static Task<KeyValue> TryGetReferencedManifestSectionAsync(
            DepotDownloader owner,
            uint appId,
            uint depotId
        )
            => Referenced(owner, appId).TryGetManifestSectionAsync(depotId);

        private async Task<KeyValue> GetRequiredDepotsSectionAsync()
        {
            var appInfo = await GetInfoAsync();
            return ProductInfoDepots.RequiredSection(this, appInfo);
        }

        private async Task<KeyValue> TryGetManifestSectionAsync(uint depotId)
        {
            var appInfo = await GetInfoAsync();
            return ProductInfoDepots.TryGetManifestSection(
                this,
                appInfo,
                depotId
            );
        }
    }
}
