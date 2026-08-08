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

            var manifest = await DepotManifestSource.GetManifestEvidenceAsync(
                Owner,
                Depot,
                DepotId
            );
            var effectiveManifestId = manifest.SelectedManifestId;
            var manifestSource = "selected";
            var manifestRequestBranch = Owner._branch;
            if (!effectiveManifestId.HasValue
                && !string.Equals(Owner._branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase)
                && manifest.PublicManifestId.HasValue)
            {
                effectiveManifestId = manifest.PublicManifestId;
                manifestSource = "public-inherited";
                manifestRequestBranch = SteamGameBranch.Public;
                Owner.Log($"Depot {DepotId} branch '{Owner._branch}' has no explicit branch manifest; inheriting public manifest {effectiveManifestId.Value}");
            }

            if (!effectiveManifestId.HasValue)
                return null;

            Owner.Log($"Found depot {DepotId} manifest {effectiveManifestId.Value} for branch '{Owner._branch}' source={manifestSource} requestBranch='{manifestRequestBranch}'");
            if (!string.Equals(Owner._branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!manifest.PublicManifestId.HasValue)
                {
                    Owner.Log($"Depot {DepotId} has no public manifest to compare with branch '{Owner._branch}'");
                }
                else if (manifest.PublicManifestId.Value == effectiveManifestId.Value)
                {
                    Owner.Log($"Depot {DepotId} branch '{Owner._branch}' uses the same effective manifest as public ({effectiveManifestId.Value}) source={manifestSource}");
                }
                else
                {
                    Owner.Log($"Depot {DepotId} branch '{Owner._branch}' differs from public: effective={effectiveManifestId.Value} public={manifest.PublicManifestId.Value} source={manifestSource}");
                }
            }

            return new DepotManifestReference(
                DepotId,
                effectiveManifestId.Value,
                Owner._branch,
                manifest.SelectedManifestId,
                manifest.PublicManifestId,
                manifestSource,
                manifestRequestBranch
            );
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
