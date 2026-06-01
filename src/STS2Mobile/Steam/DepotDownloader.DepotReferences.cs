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
        => ReadKeyValueUInt32(depot["depotfromapp"]);

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
