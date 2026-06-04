using SteamKit2;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private readonly struct ProductInfoAppSections
        {
            private readonly ProductInfoAppIdentity _identity;
            private readonly PICSProductInfo _appInfo;

            internal static ProductInfoAppSections For(
                ProductInfoAppIdentity identity,
                PICSProductInfo appInfo
            )
                => new(identity, appInfo);

            private ProductInfoAppSections(
                ProductInfoAppIdentity identity,
                PICSProductInfo appInfo
            )
            {
                _identity = identity;
                _appInfo = appInfo;
            }

            internal KeyValue RequiredDepots()
            {
                var depots = _appInfo?.KeyValues?["depots"];
                if (depots == null || depots == KeyValue.Invalid)
                    throw new System.InvalidOperationException(
                        _identity.MissingDepotsSection()
                    );

                return depots;
            }

            internal KeyValue TryManifestSection(uint depotId)
            {
                var depot = RequiredDepots()[depotId.ToString()];
                return depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
            }
        }
    }
}
