using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal void SuspendIdleTimeout()
    {
        Interlocked.Increment(ref _idleSuspendCount);
        StopIdleTimer();
    }

    internal void ResumeIdleTimeout()
    {
        if (Interlocked.Decrement(ref _idleSuspendCount) <= 0)
        {
            _idleSuspendCount = 0;
            if (IsConnected)
                ResetIdleTimer();
        }
    }

    // Enters Draining state: waits for pending RPCs to complete, then disconnects.
    internal void Flush()
    {
        lock (_stateLock)
        {
            if (!IsConnected)
                return;
            TransitionTo(ConnectionState.Draining);
            StopIdleTimer();
        }

        PatchHelper.Log("[Connection] Draining...");

        if (_sendLock.Wait(5000))
        {
            _sendLock.Release();
            DisconnectToIdle("[Connection] Drain complete, disconnected");
        }
        else
        {
            PatchHelper.Log("[Connection] Drain timed out, forcing disconnect");
            DisconnectToIdle();
        }
    }

    private void DisconnectToIdle(string completionMessage = null)
    {
        Teardown();
        TransitionTo(ConnectionState.Idle);

        if (completionMessage != null)
            PatchHelper.Log(completionMessage);
    }

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        Flush();
        _sendLock.Dispose();
        _connectedGate.Dispose();
    }
}
