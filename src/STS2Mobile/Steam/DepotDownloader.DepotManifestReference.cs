namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestReference
    {
        internal DepotManifestReference(
            uint depotId,
            ulong manifestId,
            string branch,
            ulong? selectedBranchManifestId,
            ulong? publicManifestId,
            string manifestSource,
            string manifestRequestBranch
        )
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = SteamGameBranch.Normalize(branch);
            SelectedBranchManifestId = selectedBranchManifestId;
            PublicManifestId = publicManifestId;
            ManifestSource = string.IsNullOrWhiteSpace(manifestSource)
                ? "unknown"
                : manifestSource;
            ManifestRequestBranch = SteamGameBranch.Normalize(manifestRequestBranch);
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
        internal string Branch { get; }
        internal ulong? SelectedBranchManifestId { get; }
        internal ulong? PublicManifestId { get; }
        internal string ManifestSource { get; }
        internal string ManifestRequestBranch { get; }
        internal bool HasSelectedBranchManifest => SelectedBranchManifestId.HasValue;
        internal bool HasPublicManifest => PublicManifestId.HasValue;
        internal bool EffectiveMatchesPublicManifest
            => PublicManifestId.HasValue && PublicManifestId.Value == ManifestId;
        internal bool SelectedBranchManifestMatchesPublic
            => SelectedBranchManifestId.HasValue
                && PublicManifestId.HasValue
                && SelectedBranchManifestId.Value == PublicManifestId.Value;
        internal bool InheritedFromPublic
            => string.Equals(ManifestSource, "public-inherited", System.StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct DepotManifestEvidence
    {
        internal DepotManifestEvidence(ulong? selectedManifestId, ulong? publicManifestId)
        {
            SelectedManifestId = selectedManifestId;
            PublicManifestId = publicManifestId;
        }

        internal ulong? SelectedManifestId { get; }
        internal ulong? PublicManifestId { get; }
    }
}
