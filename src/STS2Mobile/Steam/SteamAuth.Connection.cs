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

    internal void Connect()
    {
        if (_disposed || _connectedGate.IsSet || _connectStarted)
            return;

        BeginConnect("Connecting to Steam...");
    }

    private async Task<bool> WaitForConnectAsync()
    {
        var maxPolls = CurrentConnectTimeoutMs / ConnectPollDelayMs;
        for (int i = 0; i < maxPolls; i++)
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
            Connect,
            "Steam connection failed to start before auth",
            "Steam connection did not complete before auth; retrying..."
        );

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
            () => BeginConnect("Reconnecting for auth code submission..."),
            "Steam auth reconnect failed to start",
            "Steam auth reconnect did not complete; retrying..."
        );
        _needsReconnectForAuth = !connected;
        return connected;
    }

    private async Task<bool> TryConnectWithRetriesAsync(
        Action beginAttempt,
        string startFailureMessage,
        string retryMessage
    )
    {
        var retryCount = CurrentConnectRetryCount;
        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                beginAttempt();
            }
            catch (Exception ex)
            {
                Log($"{startFailureMessage}: {ex.Message}");
                ResetConnectAttemptAfterFailure();
            }

            if (await WaitForConnectAsync())
                return true;

            if (attempt < retryCount)
            {
                ResetConnectAttemptAfterFailure();
                Log(retryMessage);
                await Task.Delay(CurrentConnectRetryDelayMs);
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
