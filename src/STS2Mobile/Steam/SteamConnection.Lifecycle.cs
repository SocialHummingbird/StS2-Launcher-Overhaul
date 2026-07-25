using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void EnsureConnected(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_disposing || _disposed)
            throw new ObjectDisposedException(nameof(SteamConnection));

        if (TryUseExistingConnection())
            return;

        lock (_stateLock)
        {
            if (TryUseExistingConnection())
                return;

            cancellationToken.ThrowIfCancellationRequested();
            WaitForBackoffIfNeeded(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var attemptStarted = false;
            try
            {
                StartConnectionAttempt();
                attemptStarted = true;
                WaitForConnectionAttempt(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                CompleteConnectionAttempt();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (attemptStarted)
                {
                    PatchHelper.Log(
                        "[Connection] Connect cancelled; resetting the pending attempt"
                    );
                    ResetAfterFailedConnect();
                }

                throw;
            }
        }
    }

    private bool TryUseExistingConnection()
    {
        if (!IsConnected)
            return false;

        ResetIdleTimer();
        return true;
    }

    private void WaitForBackoffIfNeeded(
        CancellationToken cancellationToken
    )
    {
        if (State != ConnectionState.Backoff)
            return;

        PatchHelper.Log($"[Connection] Waiting {_backoffMs}ms backoff before reconnect...");
        Monitor.Exit(_stateLock);
        try
        {
            if (cancellationToken.WaitHandle.WaitOne(_backoffMs))
                cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            Monitor.Enter(_stateLock);
        }
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

    private void WaitForConnectionAttempt(
        CancellationToken cancellationToken
    )
    {
        if (!WaitForConnectedGate(cancellationToken))
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

    private bool WaitForConnectedGate(
        CancellationToken cancellationToken
    )
    {
        var deadline = Environment.TickCount64 + ConnectTimeoutMs;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
                return _connectedGate.IsSet;

            if (
                _connectedGate.Wait(
                    (int)Math.Min(50, remaining),
                    cancellationToken
                )
            )
                return true;

            if (OperatingSystem.IsAndroid())
                AndroidBridgeDispatcher.Pump();
        }
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
        ResetConnectionTransport();
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
