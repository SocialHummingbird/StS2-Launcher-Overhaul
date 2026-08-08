using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private void RegisterConnectionCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            if (_disposed)
                return;

            Log("Connected to Steam");
            MarkAuthConnected();
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(cb =>
        {
            if (_disposed)
                return;

            MarkAuthDisconnected();
            if (cb.UserInitiated)
                return;

            if (_credentialAuthStarted)
            {
                MarkAuthConnectionLostDuringCredentials();
                return;
            }

            Log("Connection lost before authentication completed - retrying");
        });
    }

    private void MarkAuthConnected()
    {
        _needsReconnectForAuth = false;
        _connectedGate.Set();
    }

    private void MarkAuthDisconnected()
    {
        _connectStarted = false;
        _connectedGate.Reset();
    }

    private void MarkAuthConnectionLostDuringCredentials()
    {
        _needsReconnectForAuth = true;

        if (!RequiresPersistentAuthConnection)
        {
            ContinueWebApiAuthWithoutPersistentConnection(
                "Steam CM connection lost during Android WebAPI authentication; will reconnect before guarded auth continues"
            );
            return;
        }

        Log("Connection lost during authentication - reconnecting to keep auth session alive");
    }
}
