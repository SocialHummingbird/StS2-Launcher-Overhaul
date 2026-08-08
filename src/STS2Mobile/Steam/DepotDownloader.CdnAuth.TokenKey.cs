using System;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnAuthTokenKey : IEquatable<CdnAuthTokenKey>
    {
        internal CdnAuthTokenKey(uint depotId, string host)
        {
            DepotId = depotId;
            Host = host;
        }

        private uint DepotId { get; }
        private string Host { get; }

        public bool Equals(CdnAuthTokenKey other)
            => DepotId == other.DepotId && Host == other.Host;

        public override bool Equals(object obj)
            => obj is CdnAuthTokenKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + DepotId.GetHashCode();
                hash = hash * 31 + Host.GetHashCode();
                return hash;
            }
        }
    }
}
