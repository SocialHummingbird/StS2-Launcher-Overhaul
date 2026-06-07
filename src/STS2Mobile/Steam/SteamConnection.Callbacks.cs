using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void RegisterCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            if (_disposing || _disposed)
                return;

            _steamUser.LogOn(CreateLogOnDetails());
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(_ =>
        {
            if (_disposing || _disposed)
                return;

            if (IsConnected)
            {
                PatchHelper.Log("[Connection] Dropped unexpectedly");
                EnterBackoff();
            }
        });

        _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(cb =>
        {
            if (_disposing || _disposed)
                return;

            if (cb.Result == EResult.OK)
            {
                _connectedGate.Set();
                return;
            }

            _connectError = new InvalidOperationException($"Login failed: {cb.Result}");
            _connectedGate.Set();
        });
    }

    private SteamUser.LogOnDetails CreateLogOnDetails()
    {
        var details = new SteamUser.LogOnDetails
        {
            Username = _accountName,
            AccessToken = _refreshToken,
            ShouldRememberPassword = true,
        };

        if (OperatingSystem.IsAndroid())
        {
            details.ClientOSType = EOSType.AndroidUnknown;
            details.MachineName = "StS2 Launcher Android";
        }

        return details;
    }
}
