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
        ulong manifestId,
        string branch
    )
    {
        var request = ManifestRequestKey.ForBranch(depotId, manifestId, branch);
        var code = await _manifestRequestCodes.GetOrAddAsync(
            request,
            ManifestRequestCodeTtl,
            () => request.FetchCodeAsync(_connection),
            code => code != 0
        );

        request.ThrowIfDenied(code);
        return code;
    }
}
