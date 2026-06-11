using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotReferenceCandidate
    {
        private DepotReferenceCandidate(DepotDownloader owner, KeyValue depot, uint depotId)
        {
            Owner = owner;
            Depot = depot;
            DepotId = depotId;
        }

        private DepotDownloader Owner { get; }
        private KeyValue Depot { get; }
        private uint DepotId { get; }

        internal static DepotReferenceCandidate? TryCreate(
            DepotDownloader owner,
            KeyValue depot
        )
            => uint.TryParse(depot.Name, out var depotId)
                ? new DepotReferenceCandidate(owner, depot, depotId)
                : null;

        internal async Task<DepotManifestReference?> TryCreateReferenceAsync()
        {
            if (ShouldSkip())
                return null;

            var manifestId = await DepotManifestSource.GetSelectedManifestIdAsync(
                Owner,
                Depot,
                DepotId
            );
            if (!manifestId.HasValue)
                return null;

            Owner.Log($"Found depot {DepotId} manifest {manifestId.Value} for branch '{Owner._branch}'");
            return new DepotManifestReference(DepotId, manifestId.Value, Owner._branch);
        }

        private bool ShouldSkip()
        {
            var config = Depot["config"];
            if (config == KeyValue.Invalid)
                return false;

            var oslist = config["oslist"]?.Value;
            if (string.IsNullOrEmpty(oslist) || oslist.Contains("windows"))
                return false;

            Owner.Log($"Skipping depot {DepotId} (OS: {oslist})");
            return true;
        }
    }

    private async Task<List<DepotManifestReference>> ParseDepotsAsync(
        KeyValue depotSection
    )
    {
        var result = new List<DepotManifestReference>();

        foreach (var depot in depotSection.Children)
        {
            var candidate = DepotReferenceCandidate.TryCreate(this, depot);
            if (!candidate.HasValue)
                continue;

            var reference = await candidate.Value.TryCreateReferenceAsync();
            if (reference.HasValue)
                result.Add(reference.Value);
        }

        return result;
    }
}
