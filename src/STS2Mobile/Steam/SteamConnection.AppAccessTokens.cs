using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).Token != 0;

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.IsDenied)
            throw new InvalidOperationException(deniedMessage);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.Token != 0)
            return tokenResult.Token;

        if (tokenResult.IsDenied)
            throw new InvalidOperationException(deniedMessage);

        return 0;
    }

    private async Task<(ulong Token, bool IsDenied)> GetAppAccessTokenAsync(uint appId)
    {
        var cachedToken = GetCachedAppAccessToken(appId);
        if (cachedToken.HasValue)
            return FoundAppAccessToken(cachedToken.Value);

        EnsureConnected();

        var tokenResult = await _steamApps.PICSGetAccessTokens(
            new[] { appId },
            Array.Empty<uint>()
        );
        if (tokenResult.AppTokens?.TryGetValue(appId, out var token) == true)
            return RememberAppAccessToken(appId, token);

        return tokenResult.AppTokensDenied?.Contains(appId) == true
            ? (Token: 0UL, IsDenied: true)
            : (Token: 0UL, IsDenied: false);
    }

    private ulong? GetCachedAppAccessToken(uint appId)
        => appId == SteamCloudApp.AppId && _appAccessToken != 0 ? _appAccessToken : null;

    private (ulong Token, bool IsDenied) RememberAppAccessToken(uint appId, ulong token)
    {
        if (appId == SteamCloudApp.AppId)
            _appAccessToken = token;

        return FoundAppAccessToken(token);
    }

    private static (ulong Token, bool IsDenied) FoundAppAccessToken(ulong token)
        => (token, IsDenied: false);
}
