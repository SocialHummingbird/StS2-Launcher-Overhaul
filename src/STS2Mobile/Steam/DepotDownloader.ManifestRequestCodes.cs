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

    private async Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId)
    {
        var key = new ManifestRequestKey(depotId, manifestId, PublicDepotBranch);
        var code = await _manifestRequestCodes.GetOrAddAsync(
            key,
            ManifestRequestCodeTtl,
            () => _connection.GetManifestRequestCodeAsync(
                depotId,
                manifestId,
                PublicDepotBranch
            ),
            code => code != 0
        );

        ThrowIfManifestRequestCodeDenied(code, depotId, manifestId, PublicDepotBranch);
        return code;
    }

    private static void ThrowIfManifestRequestCodeDenied(
        ulong code,
        uint depotId,
        ulong manifestId,
        string branch
    )
    {
        if (code != 0)
            return;

        throw new Exception(
            $"Failed to get manifest request code for depot {depotId}, "
                + $"manifest {manifestId}, branch '{branch}'. "
                + "Ensure the account owns this app."
        );
    }
}
