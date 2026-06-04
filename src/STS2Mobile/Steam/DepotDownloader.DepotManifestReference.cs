namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotManifestReference
    {
        internal DepotManifestReference(uint depotId, ulong manifestId)
        {
            DepotId = depotId;
            ManifestId = manifestId;
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
    }
}
