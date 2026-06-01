using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string PublicDepotBranch = "public";

    private readonly struct DepotReference
    {
        internal DepotReference(uint depotId, ulong manifestId)
        {
            DepotId = depotId;
            ManifestId = manifestId;
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }

        internal static DepotReference Create(uint depotId, ulong manifestId)
            => new(depotId, manifestId);
    }

    private async Task<KeyValue> GetDepotManifestSectionAsync(KeyValue depot, uint depotId)
    {
        var manifests = depot["manifests"];
        if (manifests != KeyValue.Invalid)
            return manifests;

        if (!TryGetReferencedAppId(depot, out var otherAppId))
            return KeyValue.Invalid;

        return await GetReferencedDepotManifestSectionAsync(depotId, otherAppId);
    }

    private static bool TryGetReferencedAppId(KeyValue depot, out uint otherAppId)
    {
        otherAppId = 0;
        var depotFromApp = depot["depotfromapp"];
        return depotFromApp != KeyValue.Invalid
            && depotFromApp.Value != null
            && uint.TryParse(depotFromApp.Value, out otherAppId);
    }

    private async Task<KeyValue> GetReferencedDepotManifestSectionAsync(
        uint depotId,
        uint otherAppId
    )
    {
        Log($"Depot {depotId} references app {otherAppId}, fetching...");
        var otherAppInfo = await GetAppInfoAsync(otherAppId);
        if (otherAppInfo == null)
            return KeyValue.Invalid;

        var otherDepots = GetDepotsSection(otherAppInfo, otherAppId);
        var otherDepot = otherDepots[depotId.ToString()];
        return GetManifestSectionOrInvalid(otherDepot);
    }

    private static KeyValue GetManifestSectionOrInvalid(KeyValue depot)
        => depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
}
