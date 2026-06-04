using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan ManifestRequestCodeTtl = TimeSpan.FromMinutes(5);
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

    private readonly ExpiringCache<
        ManifestRequestKey,
        ulong
    > _manifestRequestCodes = new();

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

    private Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId)
        => GetManifestRequestCodeAsync(
            new ManifestRequestCodeRequest(depotId, manifestId, PublicDepotBranch)
        );

    private async Task<ulong> GetManifestRequestCodeAsync(
        ManifestRequestCodeRequest request
    )
    {
        var code = await _manifestRequestCodes.GetOrAddAsync(
            request.Key(),
            ManifestRequestCodeTtl,
            () => request.FetchAsync(_connection),
            code => code != 0
        );

        request.ThrowIfDenied(code);
        return code;
    }
}
