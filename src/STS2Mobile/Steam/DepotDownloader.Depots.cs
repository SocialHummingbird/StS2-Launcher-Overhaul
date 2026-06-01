using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<List<DepotReference>> ParseDepotsAsync(
        KeyValue depotSection
    )
    {
        var result = new List<DepotReference>();

        foreach (var depot in depotSection.Children)
        {
            if (!uint.TryParse(depot.Name, out var depotId))
                continue;

            if (ShouldSkipDepot(depot, depotId))
                continue;

            var manifestId = await GetDepotManifestIdAsync(depot, depotId);
            if (!manifestId.HasValue)
                continue;

            Log($"Found depot {depotId} manifest {manifestId.Value}");
            result.Add(DepotReference.Create(depotId, manifestId.Value));
        }

        return result;
    }

    private bool ShouldSkipDepot(KeyValue depot, uint depotId)
    {
        var config = depot["config"];
        if (config == KeyValue.Invalid)
            return false;

        var oslist = config["oslist"]?.Value;
        if (string.IsNullOrEmpty(oslist) || oslist.Contains("windows"))
            return false;

        Log($"Skipping depot {depotId} (OS: {oslist})");
        return true;
    }

    private async Task<ulong?> GetDepotManifestIdAsync(KeyValue depot, uint depotId)
    {
        var manifests = await GetDepotManifestSectionAsync(depot, depotId);
        if (manifests == KeyValue.Invalid)
            return null;

        return TryGetPublicManifestId(manifests, out var manifestId)
            ? manifestId
            : null;
    }

    private static bool TryGetPublicManifestId(KeyValue manifests, out ulong manifestId)
    {
        manifestId = 0;
        var gidNode = manifests[PublicDepotBranch]["gid"];
        return gidNode != KeyValue.Invalid
            && gidNode.Value != null
            && ulong.TryParse(gidNode.Value, out manifestId);
    }
}
