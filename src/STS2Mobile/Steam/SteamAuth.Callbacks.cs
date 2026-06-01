using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private void RegisterConnectionCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            Log("Connected to Steam");
            _needsReconnectForAuth = false;
            _connectedGate.Set();
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(cb =>
        {
            _connectStarted = false;
            _connectedGate.Reset();
            if (cb.UserInitiated)
                return;

            if (_credentialAuthStarted)
            {
                _needsReconnectForAuth = true;
                Log("Connection lost during authentication - reconnecting to keep auth session alive");
                return;
            }

            Log("Connection lost before authentication completed - retrying");
        });
    }
}
