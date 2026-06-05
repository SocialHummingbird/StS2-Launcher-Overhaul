using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string PublicDepotBranch = "public";

    private readonly struct DepotManifestSource
    {
        private DepotManifestSource(
            DepotDownloader owner,
            KeyValue depot,
            uint depotId
        )
        {
            Owner = owner;
            Depot = depot;
            DepotId = depotId;
        }

        private DepotDownloader Owner { get; }
        private KeyValue Depot { get; }
        private uint DepotId { get; }

        internal static Task<ulong?> GetPublicManifestIdAsync(
            DepotDownloader owner,
            KeyValue depot,
            uint depotId
        )
            => new DepotManifestSource(owner, depot, depotId)
                .GetPublicManifestIdAsync();

        private async Task<ulong?> GetPublicManifestIdAsync()
        {
            var manifests = await GetManifestSectionAsync();
            return manifests == KeyValue.Invalid
                ? null
                : DepotDownloader.ReadKeyValueUInt64(
                    manifests[PublicDepotBranch]["gid"]
                );
        }

        private async Task<KeyValue> GetManifestSectionAsync()
        {
            var manifests = Depot["manifests"];
            if (manifests != KeyValue.Invalid)
                return manifests;

            var otherAppId = DepotDownloader.ReadKeyValueUInt32(
                Depot["depotfromapp"]
            );
            return otherAppId.HasValue
                ? await GetReferencedManifestSectionAsync(otherAppId.Value)
                : KeyValue.Invalid;
        }

        private async Task<KeyValue> GetReferencedManifestSectionAsync(uint appId)
        {
            Owner.Log($"Depot {DepotId} references app {appId}, fetching...");
            return await ProductInfoApp.TryGetReferencedManifestSectionAsync(
                Owner,
                appId,
                DepotId
            );
        }
    }
}
