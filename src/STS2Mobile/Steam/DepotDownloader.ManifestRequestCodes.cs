using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestRequestCodeKey
    {
        private ManifestRequestCodeKey(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = branch;
        }

        private uint DepotId { get; }
        private ulong ManifestId { get; }
        private string Branch { get; }

        internal static ManifestRequestCodeKey Public(uint depotId, ulong manifestId)
            => new(depotId, manifestId, PublicDepotBranch);
    }

    private static readonly TimeSpan ManifestRequestCodeTtl = TimeSpan.FromMinutes(5);
    private readonly ExpiringCache<ManifestRequestCodeKey, ulong> _manifestRequestCodes = new();

    private async Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId)
    {
        var key = ManifestRequestCodeKey.Public(depotId, manifestId);
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
        if (code == 0)
            throw new Exception(
                $"Failed to get manifest request code for depot {depotId}. "
                    + "Ensure the account owns this app."
            );

        return code;
    }
}
