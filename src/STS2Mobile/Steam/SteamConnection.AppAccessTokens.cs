using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).Found;

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (!tokenResult.Found && tokenResult.Denied)
            throw new InvalidOperationException(deniedMessage);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.Found)
            return tokenResult.Token;

        if (tokenResult.Denied)
            throw new InvalidOperationException(deniedMessage);

        return 0;
    }

    private async Task<AppAccessTokenResult> GetAppAccessTokenAsync(uint appId)
    {
        if (TryGetCachedAppAccessToken(appId, out var cachedToken))
            return AppAccessTokenResult.FromToken(cachedToken);

        EnsureConnected();

        var tokenResult = await _steamApps.PICSGetAccessTokens(
            new[] { appId },
            Array.Empty<uint>()
        );
        if (tokenResult.AppTokens?.TryGetValue(appId, out var token) == true)
            return RememberAppAccessToken(appId, token);

        return tokenResult.AppTokensDenied?.Contains(appId) == true
            ? AppAccessTokenResult.FromDenied()
            : AppAccessTokenResult.FromMissing();
    }

    private bool TryGetCachedAppAccessToken(uint appId, out ulong token)
    {
        token = _appAccessToken;
        return appId == SteamCloudApp.AppId && token != 0;
    }

    private AppAccessTokenResult RememberAppAccessToken(uint appId, ulong token)
    {
        if (appId == SteamCloudApp.AppId)
            _appAccessToken = token;

        return AppAccessTokenResult.FromToken(token);
    }

    private readonly struct AppAccessTokenResult
    {
        private AppAccessTokenResult(bool found, bool denied, ulong token)
        {
            Found = found;
            Denied = denied;
            Token = token;
        }

        private bool Found { get; }
        private bool Denied { get; }
        private ulong Token { get; }

        private static AppAccessTokenResult FromToken(ulong token)
            => new(found: true, denied: false, token);

        private static AppAccessTokenResult FromDenied()
            => new(found: false, denied: true, token: 0);

        private static AppAccessTokenResult FromMissing()
            => new(found: false, denied: false, token: 0);
    }
}
