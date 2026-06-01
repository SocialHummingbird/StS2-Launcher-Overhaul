using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _connectStarted = false;
        try
        {
            _client?.Disconnect();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] Disconnect failed during dispose: {ex.Message}");
        }

        _callbackPump.Stop(2000);
        _connectedGate.Dispose();
    }
}
