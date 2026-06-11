using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestRequestKey : IEquatable<ManifestRequestKey>
    {
        private ManifestRequestKey(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = branch;
        }

        private uint DepotId { get; }
        private ulong ManifestId { get; }
        private string Branch { get; }

        internal static ManifestRequestKey ForBranch(uint depotId, ulong manifestId, string branch)
            => new(depotId, manifestId, SteamGameBranch.Normalize(branch));

        internal Task<ulong> FetchCodeAsync(SteamConnection connection)
            => connection.GetManifestRequestCodeAsync(
                DepotId,
                ManifestId,
                Branch
            );

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

        public bool Equals(ManifestRequestKey other)
            => DepotId == other.DepotId
                && ManifestId == other.ManifestId
                && Branch == other.Branch;

        public override bool Equals(object obj)
            => obj is ManifestRequestKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + DepotId.GetHashCode();
                hash = hash * 31 + ManifestId.GetHashCode();
                hash = hash * 31 + Branch.GetHashCode();
                return hash;
            }
        }
    }
}
