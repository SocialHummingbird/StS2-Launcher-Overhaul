using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private readonly struct AppAccessTokenResult
    {
        private AppAccessTokenResult(bool found, bool denied, ulong token)
        {
            Found = found;
            Denied = denied;
            Token = token;
        }

        internal bool Found { get; }
        internal bool Denied { get; }
        internal ulong Token { get; }

        internal static AppAccessTokenResult FoundToken(ulong token)
            => new(true, false, token);

        internal static AppAccessTokenResult DeniedToken()
            => new(false, true, 0);

        internal static AppAccessTokenResult PublicToken()
            => new(false, false, 0);
    }

    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).Found;

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        ThrowIfAppAccessTokenDenied(tokenResult.Denied, deniedMessage);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.Found)
            return tokenResult.Token;

        ThrowIfAppAccessTokenDenied(tokenResult.Denied, deniedMessage);
        return 0;
    }

    private async Task<AppAccessTokenResult> GetAppAccessTokenAsync(uint appId)
    {
        var cachedToken = GetCachedAppAccessToken(appId);
        if (cachedToken.HasValue)
            return AppAccessTokenResult.FoundToken(cachedToken.Value);

        var tokenResult = await RunConnectedAsync(
            async () => await _steamApps.PICSGetAccessTokens(
                new[] { appId },
                Array.Empty<uint>()
            )
        ).ConfigureAwait(false);
        if (tokenResult.AppTokens?.TryGetValue(appId, out var token) == true)
            return RememberAppAccessToken(appId, token);

        return tokenResult.AppTokensDenied?.Contains(appId) == true
            ? AppAccessTokenResult.DeniedToken()
            : AppAccessTokenResult.PublicToken();
    }

    private ulong? GetCachedAppAccessToken(uint appId)
        => appId == SteamCloudApp.AppId && _appAccessToken != 0 ? _appAccessToken : null;

    private AppAccessTokenResult RememberAppAccessToken(uint appId, ulong token)
    {
        if (appId == SteamCloudApp.AppId)
            _appAccessToken = token;

        return AppAccessTokenResult.FoundToken(token);
    }

    private static void ThrowIfAppAccessTokenDenied(bool denied, string deniedMessage)
    {
        if (denied)
            throw new InvalidOperationException(deniedMessage);
    }
}
