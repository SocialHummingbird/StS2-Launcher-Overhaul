using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan ManifestRequestCodeTtl = TimeSpan.FromMinutes(5);
    private readonly ExpiringCache<
        (uint DepotId, ulong ManifestId, string Branch),
        ulong
    > _manifestRequestCodes = new();

    private async Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId)
    {
        var key = (depotId, manifestId, PublicDepotBranch);
        if (_manifestRequestCodes.TryGetFresh(key, out var cached))
            return cached;

        var code = await _connection.GetManifestRequestCodeAsync(
            depotId,
            manifestId,
            PublicDepotBranch
        );
        if (code == 0)
            throw new Exception(
                $"Failed to get manifest request code for depot {depotId}. "
                    + "Ensure the account owns this app."
            );

        _manifestRequestCodes.SetFor(key, code, ManifestRequestCodeTtl);
        return code;
    }
}
