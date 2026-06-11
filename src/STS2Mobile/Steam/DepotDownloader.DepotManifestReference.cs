namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestReference
    {
        internal DepotManifestReference(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = SteamGameBranch.Normalize(branch);
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
        internal string Branch { get; }
    }
}
