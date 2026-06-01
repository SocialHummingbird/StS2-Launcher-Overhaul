using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void RegisterCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = _accountName,
                AccessToken = _refreshToken,
                ShouldRememberPassword = true,
            });
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(_ =>
        {
            if (IsConnected)
            {
                PatchHelper.Log("[Connection] Dropped unexpectedly");
                EnterBackoff();
            }
        });

        _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(cb =>
        {
            if (cb.Result == EResult.OK)
            {
                _connectedGate.Set();
                return;
            }

            _connectError = new InvalidOperationException($"Login failed: {cb.Result}");
            _connectedGate.Set();
        });
    }
}
