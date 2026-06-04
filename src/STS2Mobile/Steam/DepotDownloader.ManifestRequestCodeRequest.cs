using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestRequestCodeRequest
    {
        internal ManifestRequestCodeRequest(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = branch;
        }

        private uint DepotId { get; }
        private ulong ManifestId { get; }
        private string Branch { get; }

        internal ManifestRequestKey Key()
            => new(DepotId, ManifestId, Branch);

        internal Task<ulong> FetchAsync(SteamConnection connection)
            => connection.GetManifestRequestCodeAsync(DepotId, ManifestId, Branch);

        internal void ThrowIfDenied(ulong code)
        {
            if (code != 0)
                return;

            throw new Exception(
                $"Failed to get manifest request code for depot {DepotId}, "
                    + $"manifest {ManifestId}, branch '{Branch}'. "
                    + "Ensure the account owns this app."
            );
        }
    }
}
