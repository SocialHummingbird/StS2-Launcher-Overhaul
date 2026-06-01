using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void ResetIdleTimer()
    {
        if (_idleSuspendCount > 0)
            return;

        StopIdleTimer();
        _idleTimer = new Timer(
            _ =>
            {
                if (IsConnected)
                {
                    PatchHelper.Log("[Connection] Idle timeout, disconnecting");
                    DisconnectToIdle();
                }
            },
            null,
            _defaultIdleTimeoutMs,
            Timeout.Infinite
        );
    }

    private void StopIdleTimer()
    {
        _idleTimer?.Dispose();
        _idleTimer = null;
    }
}
