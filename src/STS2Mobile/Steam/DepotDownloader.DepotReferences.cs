using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string PublicDepotBranch = "public";

    private async Task<KeyValue> GetDepotManifestSectionAsync(KeyValue depot, uint depotId)
    {
        var manifests = depot["manifests"];
        if (manifests != KeyValue.Invalid)
            return manifests;

        var otherAppId = GetReferencedAppId(depot);
        if (!otherAppId.HasValue)
            return KeyValue.Invalid;

        return await GetReferencedDepotManifestSectionAsync(depotId, otherAppId.Value);
    }

    private static uint? GetReferencedAppId(KeyValue depot)
    {
        var depotFromApp = depot["depotfromapp"];
        return depotFromApp != KeyValue.Invalid
            && depotFromApp.Value != null
            && uint.TryParse(depotFromApp.Value, out var otherAppId)
            ? otherAppId
            : null;
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
