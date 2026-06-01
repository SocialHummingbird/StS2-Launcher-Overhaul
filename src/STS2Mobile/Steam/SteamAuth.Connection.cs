using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    internal void Connect()
    {
        if (_connectStarted && !_connectedGate.IsSet)
            return;

        _connectStarted = true;
        _connectedGate.Reset();
        StartCallbackThread();
        _client.Connect();
        Log("Connecting to Steam...");
    }

    private async Task<bool> WaitForConnectAsync(int timeoutMs = 10_000)
    {
        for (int i = 0; i < timeoutMs / 100; i++)
        {
            if (_connectedGate.IsSet)
                return true;
            await Task.Delay(100);
        }
        return _connectedGate.IsSet;
    }

    private async Task ReconnectForAuthAsync()
    {
        Log("Reconnecting for auth code submission...");
        _needsReconnectForAuth = false;
        _connectedGate.Reset();
        _client.Connect();

        if (!await WaitForConnectAsync())
            Log("Reconnect timed out - auth code submission may fail");
    }

    private void StartCallbackThread()
    {
        _callbackPump.Start();
    }
}
