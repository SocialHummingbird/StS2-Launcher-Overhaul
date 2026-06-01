using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void EnsureConnected()
    {
        if (TryUseExistingConnection())
            return;

        lock (_stateLock)
        {
            if (TryUseExistingConnection())
                return;

            WaitForBackoffIfNeeded();
            StartConnectionAttempt();
            WaitForConnectionAttempt();
            CompleteConnectionAttempt();
        }
    }

    private bool TryUseExistingConnection()
    {
        if (!IsConnected)
            return false;

        ResetIdleTimer();
        return true;
    }

    private void WaitForBackoffIfNeeded()
    {
        if (State != ConnectionState.Backoff)
            return;

        PatchHelper.Log($"[Connection] Waiting {_backoffMs}ms backoff before reconnect...");
        Monitor.Exit(_stateLock);
        Thread.Sleep(_backoffMs);
        Monitor.Enter(_stateLock);
    }

    private void StartConnectionAttempt()
    {
        _connectError = null;
        _connectedGate.Reset();
        TransitionTo(ConnectionState.Connecting);

        try
        {
            BeginConnect();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Connection] Connect failed: {ex.Message}");
            EnterBackoff();
            throw;
        }
    }

    private void WaitForConnectionAttempt()
    {
        if (!_connectedGate.Wait(ConnectTimeoutMs))
        {
            PatchHelper.Log("[Connection] Connect timed out");
            ResetAfterFailedConnect();
            throw new TimeoutException("Steam connection timed out");
        }

        if (_connectError == null)
            return;

        ResetAfterFailedConnect();
        throw _connectError;
    }

    private void CompleteConnectionAttempt()
    {
        _backoffMs = 0;
        TransitionTo(ConnectionState.Connected);
        ResetIdleTimer();
        PatchHelper.Log("[Connection] Connected to Steam");
    }

    private void BeginConnect()
    {
        _callbackPump.Start();
        _client.Connect();
    }

    private void ResetAfterFailedConnect()
    {
        Teardown();
        EnterBackoff();
    }

    private void EnterBackoff()
    {
        _backoffMs = _backoffMs == 0 ? 2000 : Math.Min(_backoffMs * 2, MaxBackoffMs);
        TransitionTo(ConnectionState.Backoff);
    }

    private void TransitionTo(ConnectionState newState)
    {
        State = newState;
    }
}
