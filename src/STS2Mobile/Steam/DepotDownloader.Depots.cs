using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestReference
    {
        private DepotManifestReference(uint depotId, ulong manifestId)
        {
            DepotId = depotId;
            ManifestId = manifestId;
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }

        internal static DepotManifestReference Create(uint depotId, ulong manifestId)
            => new(depotId, manifestId);
    }

    private async Task<List<DepotManifestReference>> ParseDepotsAsync(
        KeyValue depotSection
    )
    {
        var result = new List<DepotManifestReference>();

        foreach (var depot in depotSection.Children)
        {
            var reference = await TryCreateDepotReferenceAsync(depot);
            if (reference.HasValue)
                result.Add(reference.Value);
        }

        return result;
    }

    private async Task<DepotManifestReference?> TryCreateDepotReferenceAsync(KeyValue depot)
    {
        if (!uint.TryParse(depot.Name, out var depotId))
            return null;

        if (ShouldSkipDepot(depot, depotId))
            return null;

        var manifestId = await GetDepotManifestIdAsync(depot, depotId);
        if (!manifestId.HasValue)
            return null;

        Log($"Found depot {depotId} manifest {manifestId.Value}");
        return DepotManifestReference.Create(depotId, manifestId.Value);
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

        return GetPublicManifestId(manifests);
    }

    private static ulong? GetPublicManifestId(KeyValue manifests)
        => ReadKeyValueUInt64(manifests[PublicDepotBranch]["gid"]);
}
