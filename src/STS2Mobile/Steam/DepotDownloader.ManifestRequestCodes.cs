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
