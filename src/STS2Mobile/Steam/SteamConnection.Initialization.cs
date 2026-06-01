using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal SteamConnection(string accountName, string refreshToken, int idleTimeoutMs = 30_000)
    {
        _accountName = accountName;
        _refreshToken = refreshToken;
        _defaultIdleTimeoutMs = idleTimeoutMs;

        var config = SteamConnectionConfigurationFactory.Create();
        _client = new SteamClient(config);
        _callbackManager = new CallbackManager(_client);
        _steamUser = _client.GetHandler<SteamUser>();
        _steamApps = _client.GetHandler<SteamApps>();
        _steamContent = _client.GetHandler<SteamContent>();
        _unifiedMessages = _client.GetHandler<SteamUnifiedMessages>();
        _unifiedMessages.CreateService<Cloud>();
        _callbackPump = new SteamCallbackPump(
            _callbackManager,
            "SteamConnectionCallbacks",
            msg => PatchHelper.Log($"[Connection] {msg}"),
            EnterBackoff);

        RegisterCallbacks();
    }
}
