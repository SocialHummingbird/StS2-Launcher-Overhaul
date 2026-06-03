using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestLookup
    {
        private DepotManifestLookup(KeyValue depot, uint depotId)
        {
            Depot = depot;
            DepotId = depotId;
        }

        private KeyValue Depot { get; }
        private uint DepotId { get; }

        internal static DepotManifestLookup Create(KeyValue depot, uint depotId)
            => new(depot, depotId);

        internal async Task<ulong?> GetPublicManifestIdAsync(DepotDownloader owner)
        {
            var manifests = await GetManifestSectionAsync(owner);
            return manifests == KeyValue.Invalid
                ? null
                : ReadKeyValueUInt64(manifests[PublicDepotBranch]["gid"]);
        }

        private async Task<KeyValue> GetManifestSectionAsync(DepotDownloader owner)
        {
            var manifests = Depot["manifests"];
            if (manifests != KeyValue.Invalid)
                return manifests;

            var otherAppId = ReferencedAppId();
            return otherAppId.HasValue
                ? await GetReferencedManifestSectionAsync(owner, otherAppId.Value)
                : KeyValue.Invalid;
        }

        private uint? ReferencedAppId()
            => ReadKeyValueUInt32(Depot["depotfromapp"]);

        private async Task<KeyValue> GetReferencedManifestSectionAsync(
            DepotDownloader owner,
            uint otherAppId
        )
        {
            owner.Log($"Depot {DepotId} references app {otherAppId}, fetching...");
            var app = ProductInfoApp.Create(otherAppId);
            var otherAppInfo = await owner.GetAppInfoAsync(app);
            if (otherAppInfo == null)
                return KeyValue.Invalid;

            var otherDepots = app.GetDepotsSection(otherAppInfo);
            var otherDepot = otherDepots[DepotId.ToString()];
            return GetManifestSectionOrInvalid(otherDepot);
        }

        private static KeyValue GetManifestSectionOrInvalid(KeyValue depot)
            => depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
    }

    private const string PublicDepotBranch = "public";

    private static uint? ReadKeyValueUInt32(KeyValue value)
        => value != KeyValue.Invalid
            && value.Value != null
            && uint.TryParse(value.Value, out var parsed)
            ? parsed
            : null;

    private static ulong? ReadKeyValueUInt64(KeyValue value)
        => value != KeyValue.Invalid
            && value.Value != null
            && ulong.TryParse(value.Value, out var parsed)
            ? parsed
            : null;
}
