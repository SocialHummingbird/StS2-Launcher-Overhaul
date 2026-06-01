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
        private ulong Token { get; }
        private bool HasToken => _state == TokenState.Found;
        private bool IsDenied => _state == TokenState.Denied;

        internal static AppAccessTokenResult Found(ulong token)
            => new(TokenState.Found, token);

        internal static AppAccessTokenResult Denied()
            => new(TokenState.Denied, 0);

        internal static AppAccessTokenResult Public()
            => new(TokenState.Public, 0);

        internal bool HasUsableToken()
            => HasToken;

        internal void ThrowIfDenied(string deniedMessage)
        {
            if (IsDenied)
                throw new InvalidOperationException(deniedMessage);
        }

        internal ulong TokenOrPublic(string deniedMessage)
        {
            if (HasToken)
                return Token;

            ThrowIfDenied(deniedMessage);
            return 0;
        }
    }

    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).HasUsableToken();

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        tokenResult.ThrowIfDenied(deniedMessage);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
    {
        var tokenResult = await GetAppAccessTokenAsync(appId);
        return tokenResult.TokenOrPublic(deniedMessage);
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
