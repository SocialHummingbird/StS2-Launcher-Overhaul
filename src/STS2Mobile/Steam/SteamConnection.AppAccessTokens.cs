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

    private readonly struct AppAccessTokenRequest
    {
        internal AppAccessTokenRequest(uint appId)
        {
            AppId = appId;
        }

        private uint AppId { get; }

        internal async Task<AppAccessTokenResult> GetAsync(SteamConnection owner)
        {
            var appId = AppId;
            var cachedToken = CachedToken(owner);
            if (cachedToken.HasValue)
                return AppAccessTokenResult.FoundToken(cachedToken.Value);

            var tokenResult = await owner.RunConnectedAsync(
                async () => await owner._steamApps.PICSGetAccessTokens(
                    new[] { appId },
                    Array.Empty<uint>()
                )
            ).ConfigureAwait(false);

            if (tokenResult.AppTokens?.TryGetValue(appId, out var token) == true)
                return Remember(owner, token);

            return tokenResult.AppTokensDenied?.Contains(appId) == true
                ? AppAccessTokenResult.DeniedToken()
                : AppAccessTokenResult.PublicToken();
        }

        private ulong? CachedToken(SteamConnection owner)
            => IsMainApp() && owner._appAccessToken != 0 ? owner._appAccessToken : null;

        private AppAccessTokenResult Remember(SteamConnection owner, ulong token)
        {
            if (IsMainApp())
                owner._appAccessToken = token;

            return AppAccessTokenResult.FoundToken(token);
        }

        private bool IsMainApp()
            => AppId == SteamCloudApp.AppId;
    }

    internal async Task<bool> HasAppAccessTokenAsync(uint appId)
        => (await GetAppAccessTokenAsync(appId)).HasToken();

    internal async Task EnsureAppAccessTokenNotDeniedAsync(uint appId, string deniedMessage)
        => (await GetAppAccessTokenAsync(appId)).ThrowIfDenied(deniedMessage);

    internal async Task<ulong> GetAppAccessTokenOrPublicAsync(uint appId, string deniedMessage)
        => (await GetAppAccessTokenAsync(appId)).TokenOrPublic(deniedMessage);

    private async Task<AppAccessTokenResult> GetAppAccessTokenAsync(uint appId)
        => await new AppAccessTokenRequest(appId)
            .GetAsync(this)
            .ConfigureAwait(false);
}
