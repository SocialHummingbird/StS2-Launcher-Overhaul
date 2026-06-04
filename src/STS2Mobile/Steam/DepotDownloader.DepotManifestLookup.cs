using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestLookup
    {
        private readonly DepotDownloader _owner;
        private readonly KeyValue _depot;
        private readonly uint _depotId;

        private DepotManifestLookup(DepotDownloader owner, KeyValue depot, uint depotId)
        {
            _owner = owner;
            _depot = depot;
            _depotId = depotId;
        }

        internal static DepotManifestLookup For(
            DepotDownloader owner,
            KeyValue depot,
            uint depotId
        )
            => new(owner, depot, depotId);

        internal async Task<ulong?> GetPublicManifestIdAsync()
        {
            var manifests = await GetManifestSectionAsync();
            return manifests == KeyValue.Invalid
                ? null
                : ReadKeyValueUInt64(manifests[PublicDepotBranch]["gid"]);
        }

        private async Task<KeyValue> GetManifestSectionAsync()
        {
            var manifests = _depot["manifests"];
            if (manifests != KeyValue.Invalid)
                return manifests;

            var otherAppId = ReadKeyValueUInt32(_depot["depotfromapp"]);
            return otherAppId.HasValue
                ? await GetReferencedManifestSectionAsync(otherAppId.Value)
                : KeyValue.Invalid;
        }

        private async Task<KeyValue> GetReferencedManifestSectionAsync(uint appId)
        {
            _owner.Log($"Depot {_depotId} references app {appId}, fetching...");
            return await ProductInfoApp.TryGetReferencedManifestSectionAsync(
                _owner,
                appId,
                _depotId
            );
        }
    }
}
