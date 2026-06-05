using System;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private readonly struct AppAccessTokenResult
    {
        private enum State
        {
            Public,
            Found,
            Denied,
        }

        private AppAccessTokenResult(State state, ulong token)
        {
            TokenState = state;
            Token = token;
        }

        private State TokenState { get; }
        private ulong Token { get; }

        private bool HasToken()
            => TokenState == State.Found;

        private void ThrowIfDenied(string deniedMessage)
        {
            if (TokenState == State.Denied)
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
            => new(State.Found, token);

        internal static AppAccessTokenResult DeniedToken()
            => new(State.Denied, 0);

        internal static AppAccessTokenResult PublicToken()
            => new(State.Public, 0);
    }

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
        => (await GetAppAccessTokenAsync(appId)).TokenOrPublic(deniedMessage);

    private async Task<AppAccessTokenResult> GetAppAccessTokenAsync(uint appId)
    {
        var cachedToken = CachedAppAccessToken(appId);
        if (cachedToken.HasValue)
            return AppAccessTokenResult.FoundToken(cachedToken.Value);

        return await FetchAppAccessTokenAsync(appId);
    }

    private async Task<AppAccessTokenResult> FetchAppAccessTokenAsync(uint appId)
    {
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

    private ulong? CachedAppAccessToken(uint appId)
        => IsMainApp(appId) && _appAccessToken != 0 ? _appAccessToken : null;

    private AppAccessTokenResult RememberAppAccessToken(uint appId, ulong token)
    {
        if (IsMainApp(appId))
            _appAccessToken = token;

        return AppAccessTokenResult.FoundToken(token);
    }

    private static bool IsMainApp(uint appId)
        => appId == SteamCloudApp.AppId;
}
