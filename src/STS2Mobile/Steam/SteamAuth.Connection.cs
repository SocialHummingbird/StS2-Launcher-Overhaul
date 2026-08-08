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
    private const int ConnectPollDelayMs = 100;

    private readonly struct ConnectRetryAttempt
    {
        internal ConnectRetryAttempt(
            Action begin,
            string startFailureMessage,
            string retryMessage
        )
        {
            Begin = begin;
            StartFailureMessage = startFailureMessage;
            RetryMessage = retryMessage;
        }

        private Action Begin { get; }
        private string StartFailureMessage { get; }
        private string RetryMessage { get; }

        internal void BeginOrResetAfterFailure(SteamAuth owner)
        {
            try
            {
                Begin();
            }
            catch (Exception ex)
            {
                owner.Log($"{StartFailureMessage}: {ex.Message}");
                owner.ResetConnectAttemptAfterFailure();
            }
        }

        internal async Task DelayBeforeRetryAsync(SteamAuth owner, int retryDelayMs)
        {
            owner.ResetConnectAttemptAfterFailure();
            owner.Log(RetryMessage);
            await Task.Delay(retryDelayMs);
        }
    }

    private readonly struct ConnectRetryPolicy
    {
        private ConnectRetryPolicy(int retryCount, int retryDelayMs, int timeoutMs)
        {
            RetryCount = retryCount;
            RetryDelayMs = retryDelayMs;
            TimeoutMs = timeoutMs;
        }

        internal int RetryCount { get; }
        internal int RetryDelayMs { get; }
        internal int TimeoutMs { get; }
        internal int MaxPolls => TimeoutMs / ConnectPollDelayMs;

        internal static ConnectRetryPolicy Current()
            => OperatingSystem.IsAndroid()
                ? new(
                    AndroidConnectRetryCount,
                    AndroidConnectRetryDelayMs,
                    AndroidConnectTimeoutMs
                )
                : new(
                    ConnectRetryCount,
                    ConnectRetryDelayMs,
                    ConnectTimeoutMs
                );
    }

    private readonly struct ConnectRetryRunner
    {
        private ConnectRetryRunner(SteamAuth owner, ConnectRetryAttempt retry)
        {
            Owner = owner;
            Retry = retry;
            Policy = ConnectRetryPolicy.Current();
        }

        private SteamAuth Owner { get; }
        private ConnectRetryAttempt Retry { get; }
        private ConnectRetryPolicy Policy { get; }

        internal static Task<bool> RunAsync(SteamAuth owner, ConnectRetryAttempt retry)
            => new ConnectRetryRunner(owner, retry).RunAsync();

        private async Task<bool> RunAsync()
        {
            for (int attempt = 1; attempt <= Policy.RetryCount; attempt++)
            {
                Retry.BeginOrResetAfterFailure(Owner);

                if (await Owner.WaitForConnectAsync(Policy))
                    return true;

                if (attempt < Policy.RetryCount)
                    await Retry.DelayBeforeRetryAsync(Owner, Policy.RetryDelayMs);
            }

            return false;
        }
    }

    internal void Connect()
    {
        if (_disposed || _connectedGate.IsSet || _connectStarted)
            return;

        BeginConnect("Connecting to Steam...");
    }

    private async Task<bool> WaitForConnectAsync(ConnectRetryPolicy policy)
    {
        for (int i = 0; i < policy.MaxPolls; i++)
        {
            if (_connectedGate.IsSet)
                return true;
            if (!_connectStarted)
                return false;
            await Task.Delay(ConnectPollDelayMs);
        }

        return _connectedGate.IsSet;
    }

    private Task<bool> ConnectWithRetriesAsync()
        => TryConnectWithRetriesAsync(
            new ConnectRetryAttempt(
                Connect,
                "Steam connection failed to start before auth",
                "Steam connection did not complete before auth; retrying..."
            )
        );

    private async Task ForceReconnectForLoginRetryAsync(string message)
    {
        Log(message);
        ResetConnectAttemptAfterFailure(force: true);
        await EnsureConnectedForLoginAsync();
    }

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

        var connected = await TryConnectWithRetriesAsync(
            new ConnectRetryAttempt(
                () => BeginConnect("Reconnecting for auth code submission..."),
                "Steam auth reconnect failed to start",
                "Steam auth reconnect did not complete; retrying..."
            )
        );
        _needsReconnectForAuth = !connected;
        return connected;
    }

    private async Task<bool> TryConnectWithRetriesAsync(ConnectRetryAttempt retry)
        => await ConnectRetryRunner.RunAsync(this, retry);

    private void BeginConnect(string message)
    {
        _connectStarted = true;
        _connectedGate.Reset();
        StartCallbackThread();
        _client.Connect();
        Log(message);
    }

    private void ResetConnectAttemptAfterFailure(bool force = false)
    {
        if (!_connectStarted && !force)
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

}
