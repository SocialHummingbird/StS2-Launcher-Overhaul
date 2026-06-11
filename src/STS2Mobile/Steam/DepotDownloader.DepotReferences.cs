using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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

        internal static Task<ulong?> GetSelectedManifestIdAsync(
            DepotDownloader owner,
            KeyValue depot,
            uint depotId
        )
            => new DepotManifestSource(owner, depot, depotId)
                .GetSelectedManifestIdAsync();

        private async Task<ulong?> GetSelectedManifestIdAsync()
        {
            var manifests = await GetManifestSectionAsync();
            if (manifests == KeyValue.Invalid)
                return null;

            var manifestId = DepotDownloader.ReadKeyValueUInt64(
                manifests[Owner._branch]["gid"]
            );
            if (!manifestId.HasValue)
                Owner.Log($"Depot {DepotId} has no manifest for branch '{Owner._branch}'");

            return manifestId;
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
