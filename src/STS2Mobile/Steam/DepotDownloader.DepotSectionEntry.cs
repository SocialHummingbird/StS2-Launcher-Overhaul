using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotSectionEntry
    {
        private readonly KeyValue _depot;

        private DepotSectionEntry(KeyValue depot, uint depotId)
        {
            _depot = depot;
            DepotId = depotId;
        }

        internal uint DepotId { get; }

        internal static bool TryCreate(KeyValue depot, out DepotSectionEntry entry)
        {
            if (uint.TryParse(depot.Name, out var depotId))
            {
                entry = new DepotSectionEntry(depot, depotId);
                return true;
            }

            entry = default;
            return false;
        }

        internal bool ShouldSkip(DepotDownloader owner)
        {
            var config = _depot["config"];
            if (config == KeyValue.Invalid)
                return false;

            var oslist = config["oslist"]?.Value;
            if (string.IsNullOrEmpty(oslist) || oslist.Contains("windows"))
                return false;

            owner.Log($"Skipping depot {DepotId} (OS: {oslist})");
            return true;
        }

        internal Task<ulong?> GetPublicManifestIdAsync(DepotDownloader owner)
            => owner.GetPublicManifestIdAsync(_depot, DepotId);
    }
}
