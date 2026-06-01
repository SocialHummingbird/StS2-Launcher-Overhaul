using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private readonly struct AppAccessTokenResult
    {
        private enum TokenState
        {
            Public,
            Found,
            Denied,
        }

        private AppAccessTokenResult(TokenState state, ulong token)
        {
            _state = state;
            Token = token;
        }

        private readonly TokenState _state;
        internal ulong Token { get; }
        internal bool HasToken => _state == TokenState.Found;
        internal bool IsDenied => _state == TokenState.Denied;

        internal static AppAccessTokenResult Found(ulong token)
            => new(TokenState.Found, token);

        internal static AppAccessTokenResult Denied()
            => new(TokenState.Denied, 0);

        internal static AppAccessTokenResult Public()
            => new(TokenState.Public, 0);
    }

    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).HasToken;

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.IsDenied)
            throw new InvalidOperationException(deniedMessage);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        if (tokenResult.HasToken)
            return tokenResult.Token;

        if (tokenResult.IsDenied)
            throw new InvalidOperationException(deniedMessage);

        return 0;
    }

    private async Task<AppAccessTokenResult> GetAppAccessTokenAsync(uint appId)
    {
        var cachedToken = GetCachedAppAccessToken(appId);
        if (cachedToken.HasValue)
            return AppAccessTokenResult.Found(cachedToken.Value);

        var tokenResult = await RunConnectedAsync(
            async () => await _steamApps.PICSGetAccessTokens(
                new[] { appId },
                Array.Empty<uint>()
            )
        ).ConfigureAwait(false);
        if (tokenResult.AppTokens?.TryGetValue(appId, out var token) == true)
            return RememberAppAccessToken(appId, token);

        return tokenResult.AppTokensDenied?.Contains(appId) == true
            ? AppAccessTokenResult.Denied()
            : AppAccessTokenResult.Public();
    }

    private ulong? GetCachedAppAccessToken(uint appId)
        => appId == SteamCloudApp.AppId && _appAccessToken != 0 ? _appAccessToken : null;

    private AppAccessTokenResult RememberAppAccessToken(uint appId, ulong token)
    {
        if (appId == SteamCloudApp.AppId)
            _appAccessToken = token;

        return AppAccessTokenResult.Found(token);
    }
}
