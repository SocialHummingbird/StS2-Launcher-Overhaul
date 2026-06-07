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
        if (_disposed)
            return;

        lock (_stateLock)
        {
            if (!IsConnected)
                return;
            TransitionTo(ConnectionState.Draining);
            StopIdleTimer();
        }

        PatchHelper.Log("[Connection] Draining...");

        if (WaitForDrain())
        {
            DisconnectToIdle("[Connection] Drain complete, disconnected");
        }
        else
        {
            PatchHelper.Log("[Connection] Drain timed out, forcing disconnect");
            DisconnectToIdle();
        }
    }

    private bool WaitForDrain()
    {
        if (!_sendLock.Wait(5000))
            return false;

        _sendLock.Release();
        return true;
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
        if (_disposed)
            return;

        _disposing = true;
        Flush();
        var callbackPumpStopped = Teardown();
        _disposed = true;
        _sendLock.Dispose();
        if (callbackPumpStopped)
            _connectedGate.Dispose();
        else
            PatchHelper.Log("[Connection] Leaving gate undisposed because callbacks may still be running");
    }
}
