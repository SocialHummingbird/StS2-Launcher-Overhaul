using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan ManifestRequestCodeTtl = TimeSpan.FromMinutes(5);
    private readonly ExpiringCache<
        ManifestRequestKey,
        ulong
    > _manifestRequestCodes = new();

    private async Task<ulong> GetManifestRequestCodeAsync(
        uint depotId,
        ulong manifestId
    )
    {
        var branch = PublicDepotBranch;
        var code = await _manifestRequestCodes.GetOrAddAsync(
            new ManifestRequestKey(depotId, manifestId, branch),
            ManifestRequestCodeTtl,
            () => _connection.GetManifestRequestCodeAsync(
                depotId,
                manifestId,
                branch
            ),
            code => code != 0
        );

        ThrowIfManifestRequestCodeDenied(code, depotId, manifestId, branch);
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
