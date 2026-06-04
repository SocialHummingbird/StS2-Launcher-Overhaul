using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private enum AppAccessTokenState
    {
        Public,
        Found,
        Denied,
    }

    private readonly struct AppAccessTokenResult
    {
        private AppAccessTokenResult(AppAccessTokenState state, ulong token)
        {
            State = state;
            Token = token;
        }

        private AppAccessTokenState State { get; }
        private ulong Token { get; }

        internal bool HasToken()
            => State == AppAccessTokenState.Found;

        internal void ThrowIfDenied(string deniedMessage)
        {
            if (State == AppAccessTokenState.Denied)
                throw new InvalidOperationException(deniedMessage);
        }

        internal ulong TokenOrPublic(string deniedMessage)
        {
            if (HasToken())
                return Token;

            ThrowIfDenied(deniedMessage);
            return 0;
        }

        internal static AppAccessTokenResult FoundToken(ulong token)
            => new(AppAccessTokenState.Found, token);

        internal static AppAccessTokenResult DeniedToken()
            => new(AppAccessTokenState.Denied, 0);

        internal static AppAccessTokenResult PublicToken()
            => new(AppAccessTokenState.Public, 0);
    }

    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).HasToken();

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
        => (await GetAppAccessTokenAsync(appId)).ThrowIfDenied(deniedMessage);

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
        => (await GetAppAccessTokenAsync(appId)).TokenOrPublic(deniedMessage);

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
}
