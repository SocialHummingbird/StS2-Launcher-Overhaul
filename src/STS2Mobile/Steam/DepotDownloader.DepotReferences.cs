using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string PublicDepotBranch = "public";

    private async Task<ulong?> GetPublicManifestIdAsync(
        KeyValue depot,
        uint depotId
    )
    {
        var manifests = await GetManifestSectionAsync(depot, depotId);
        return manifests == KeyValue.Invalid
            ? null
            : ReadKeyValueUInt64(manifests[PublicDepotBranch]["gid"]);
    }

    private async Task<KeyValue> GetManifestSectionAsync(
        KeyValue depot,
        uint depotId
    )
    {
        var manifests = depot["manifests"];
        if (manifests != KeyValue.Invalid)
            return manifests;

        var otherAppId = ReadKeyValueUInt32(depot["depotfromapp"]);
        return otherAppId.HasValue
            ? await GetReferencedManifestSectionAsync(otherAppId.Value, depotId)
            : KeyValue.Invalid;
    }

    private async Task<KeyValue> GetReferencedManifestSectionAsync(
        uint appId,
        uint depotId
    )
    {
        Log($"Depot {depotId} references app {appId}, fetching...");
        return await ProductInfoApp.TryGetReferencedManifestSectionAsync(
            this,
            appId,
            depotId
        );
    }
}
