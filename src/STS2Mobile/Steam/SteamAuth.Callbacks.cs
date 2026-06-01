using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private void RegisterConnectionCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            Log("Connected to Steam");
            _connectedGate.Set();
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(cb =>
        {
            _connectedGate.Reset();
            if (!cb.UserInitiated)
            {
                _needsReconnectForAuth = true;
                Log("Connection lost during authentication - will reconnect on code submit");
            }
        });
    }
}
