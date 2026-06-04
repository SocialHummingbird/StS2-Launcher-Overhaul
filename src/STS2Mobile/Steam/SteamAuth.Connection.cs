using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int ConnectRetryCount = 3;
    private const int AndroidConnectRetryCount = 5;
    private const int ConnectRetryDelayMs = 500;
    private const int AndroidConnectRetryDelayMs = 1000;
    private const int ConnectTimeoutMs = 10_000;
    private const int AndroidConnectTimeoutMs = 30_000;

    private readonly struct ConnectRetryPlan
    {
        internal ConnectRetryPlan(
            Action beginAttempt,
            string startFailureMessage,
            string retryMessage,
            int retryCount,
            int retryDelayMs
        )
        {
            BeginAttempt = beginAttempt;
            StartFailureMessage = startFailureMessage;
            RetryMessage = retryMessage;
            RetryCount = retryCount;
            RetryDelayMs = retryDelayMs;
        }

        private Action BeginAttempt { get; }
        private string StartFailureMessage { get; }
        private string RetryMessage { get; }
        private int RetryCount { get; }
        private int RetryDelayMs { get; }

        internal void Begin()
            => BeginAttempt();

        internal void LogStartFailure(SteamAuth auth, Exception ex)
            => auth.Log($"{StartFailureMessage}: {ex.Message}");

        internal void LogRetry(SteamAuth auth)
            => auth.Log(RetryMessage);

        internal bool HasRetryAfter(int attempt)
            => attempt < RetryCount;

        internal bool IncludesAttempt(int attempt)
            => attempt <= RetryCount;

        internal Task WaitBeforeRetryAsync()
            => Task.Delay(RetryDelayMs);

        internal static ConnectRetryPlan Initial(SteamAuth auth)
            => new(
                auth.Connect,
                "Steam connection failed to start before auth",
                "Steam connection did not complete before auth; retrying...",
                CurrentConnectRetryCount,
                CurrentConnectRetryDelayMs
            );

        internal static ConnectRetryPlan AuthReconnect(SteamAuth auth)
            => new(
                () => auth.BeginConnect("Reconnecting for auth code submission..."),
                "Steam auth reconnect failed to start",
                "Steam auth reconnect did not complete; retrying...",
                CurrentConnectRetryCount,
                CurrentConnectRetryDelayMs
            );
    }

    internal void Connect()
    {
        if (_disposed || _connectedGate.IsSet || _connectStarted)
            return;

        BeginConnect("Connecting to Steam...");
    }

    private async Task<bool> WaitForConnectAsync()
    {
        var timeoutMs = CurrentConnectTimeoutMs;
        for (int i = 0; i < timeoutMs / 100; i++)
        {
            if (_connectedGate.IsSet)
                return true;
            if (!_connectStarted)
                return false;
            await Task.Delay(100);
        }
        return _connectedGate.IsSet;
    }

    private Task<bool> ConnectWithRetriesAsync()
        => TryConnectWithRetriesAsync(ConnectRetryPlan.Initial(this));

    private async Task ReconnectForAuthAsync()
    {
        if (await TryReconnectForAuthAsync())
            return;

        throw new TimeoutException(
            "Steam disconnected during authentication and could not reconnect. "
                + "Try again; if Steam Guard is required, keep the launcher open and submit the code promptly."
        );
    }

    private async Task<bool> TryReconnectForAuthAsync()
    {
        _needsReconnectForAuth = false;

        var connected = await TryConnectWithRetriesAsync(ConnectRetryPlan.AuthReconnect(this));
        _needsReconnectForAuth = !connected;
        return connected;
    }

    private async Task<bool> TryConnectWithRetriesAsync(ConnectRetryPlan plan)
    {
        for (int attempt = 1; plan.IncludesAttempt(attempt); attempt++)
        {
            try
            {
                plan.Begin();
            }
            catch (Exception ex)
            {
                plan.LogStartFailure(this, ex);
                ResetConnectAttemptAfterFailure();
            }

            if (await WaitForConnectAsync())
                return true;

            if (plan.HasRetryAfter(attempt))
            {
                ResetConnectAttemptAfterFailure();
                plan.LogRetry(this);
                await plan.WaitBeforeRetryAsync();
            }
        }

        return false;
    }

    private void BeginConnect(string message)
    {
        _connectStarted = true;
        _connectedGate.Reset();
        StartCallbackThread();
        _client.Connect();
        Log(message);
    }

    private void ResetConnectAttemptAfterFailure()
    {
        if (!_connectStarted)
            return;

        _connectStarted = false;
        _connectedGate.Reset();
        try
        {
            _client.Disconnect();
        }
        catch (Exception ex)
        {
            Log($"Disconnect failed before auth retry: {ex.Message}");
        }
    }

    private void StartCallbackThread()
    {
        _callbackPump.Start();
    }

    private static int CurrentConnectRetryCount
        => OperatingSystem.IsAndroid() ? AndroidConnectRetryCount : ConnectRetryCount;

    private static int CurrentConnectRetryDelayMs
        => OperatingSystem.IsAndroid() ? AndroidConnectRetryDelayMs : ConnectRetryDelayMs;

    private static int CurrentConnectTimeoutMs
        => OperatingSystem.IsAndroid() ? AndroidConnectTimeoutMs : ConnectTimeoutMs;
}
