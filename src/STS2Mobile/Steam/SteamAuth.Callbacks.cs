using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private void RegisterConnectionCallbacks()
    {
        _callbackManager.Subscribe<SteamClient.ConnectedCallback>(_ =>
        {
            Log("Connected to Steam");
            MarkAuthConnected();
        });

        _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(cb =>
        {
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
        Log(AuthConnectionLostDuringCredentialsMessage());
    }

    private static string AuthConnectionLostDuringCredentialsMessage()
        => RequiresPersistentAuthConnection
            ? "Connection lost during authentication - reconnecting to keep auth session alive"
            : "Steam CM connection lost during Android WebAPI authentication; continuing credential flow";
}
