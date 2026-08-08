using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal Task<ulong> GetAuthenticatedSteamId64Async(
        CancellationToken cancellationToken
    )
    {
        EnsureConnected(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var steamId = _steamUser.SteamID;
        if (steamId == null || !steamId.IsValid)
        {
            throw new InvalidOperationException(
                "Steam authentication completed without a valid SteamID64"
            );
        }

        var steamId64 = steamId.ConvertToUInt64();
        if (steamId64 == 0)
        {
            throw new InvalidOperationException(
                "Steam authentication returned an empty SteamID64"
            );
        }

        return Task.FromResult(steamId64);
    }
}
