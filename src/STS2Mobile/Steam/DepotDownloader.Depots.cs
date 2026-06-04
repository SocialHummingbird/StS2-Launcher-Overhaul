using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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
        if (!DepotSectionEntry.TryCreate(depot, out var entry))
            return null;

        if (entry.ShouldSkip(this))
            return null;

        var manifestId = await entry.GetPublicManifestIdAsync(this);
        if (!manifestId.HasValue)
            return null;

        Log($"Found depot {entry.DepotId} manifest {manifestId.Value}");
        return new DepotManifestReference(entry.DepotId, manifestId.Value);
    }
}
