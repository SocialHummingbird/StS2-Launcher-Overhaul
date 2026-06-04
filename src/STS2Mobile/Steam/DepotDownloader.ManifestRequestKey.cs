using System;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestRequestKey : IEquatable<ManifestRequestKey>
    {
        internal ManifestRequestKey(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = branch;
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
        internal string Branch { get; }

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
