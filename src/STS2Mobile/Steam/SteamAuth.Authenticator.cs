using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int AuthReconnectPollDelayMs = 250;

    private readonly struct AuthCodeRequest
    {
        private AuthCodeRequest(string retryMessage, string initialMessage)
        {
            RetryMessage = retryMessage;
            InitialMessage = initialMessage;
        }

        internal string RetryMessage { get; }
        internal string InitialMessage { get; }

        internal static AuthCodeRequest Device()
            => new(
                "Previous 2FA code was incorrect, requesting new code",
                "Steam Guard 2FA code required"
            );

        internal static AuthCodeRequest Email(string email)
            => new(
                "Previous email code was incorrect, requesting new code",
                $"Steam Guard email code sent to {email}"
            );
    }

    Task<string> IAuthenticator.GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        => RequestCodeAsync(previousCodeWasIncorrect, AuthCodeRequest.Device());

    Task<string> IAuthenticator.GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        => RequestCodeAsync(previousCodeWasIncorrect, AuthCodeRequest.Email(email));

    Task<bool> IAuthenticator.AcceptDeviceConfirmationAsync()
    {
        Log("Steam mobile app confirmation requested; using Steam Guard code entry");
        return Task.FromResult(false);
    }

    private async Task<string> RequestCodeAsync(
        bool previousCodeWasIncorrect,
        AuthCodeRequest request
    )
    {
        Log(previousCodeWasIncorrect ? request.RetryMessage : request.InitialMessage);

        _waitingForAuthCode = true;
        try
        {
            var code = await WaitForTaskAndMaintainAuthConnectionAsync(
                _codeProvider(previousCodeWasIncorrect),
                () => NeedsAuthReconnect,
                "Steam auth reconnect did not complete yet; will retry while waiting for code"
            );
            await ReconnectBeforeCodeSubmitAsync();
            return code;
        }
        finally
        {
            _waitingForAuthCode = false;
        }
    }

    private async Task<T> WaitForTaskAndMaintainAuthConnectionAsync<T>(
        Task<T> task,
        Func<bool> shouldReconnect,
        string reconnectRetryMessage
    )
    {
        while (true)
        {
            if (task.IsCompleted)
                return await task;

            if (shouldReconnect())
            {
                await MaintainAuthConnectionAsync(reconnectRetryMessage);
            }

            await Task.WhenAny(task, Task.Delay(AuthReconnectPollDelayMs));
        }
    }

    private async Task MaintainAuthConnectionAsync(string reconnectRetryMessage)
    {
        if (!RequiresPersistentAuthConnection)
        {
            LogAndroidAuthConnectionLossOnce();
            return;
        }

        if (!await TryReconnectForAuthAsync())
            Log(reconnectRetryMessage);
    }

    private async Task ReconnectBeforeCodeSubmitAsync()
    {
        if (!NeedsAuthReconnect)
            return;

        if (ContinueWebApiAuthWithoutPersistentConnection(
                "Submitting Steam Guard code through Steam WebAPI without CM reconnect"
            ))
        {
            return;
        }

        await ReconnectForAuthAsync();
    }

    private bool ContinueWebApiAuthWithoutPersistentConnection(string message)
    {
        if (RequiresPersistentAuthConnection)
            return false;

        LogAndroidAuthConnectionLossOnce();
        Log(message);
        return true;
    }

    private bool NeedsAuthReconnect => _needsReconnectForAuth || !_connectedGate.IsSet;

    private void LogAndroidAuthConnectionLossOnce()
    {
        if (_androidAuthConnectionLossLogged)
            return;

        _androidAuthConnectionLossLogged = true;
        Log("Steam CM connection unavailable during Android WebAPI auth; continuing credential flow");
    }
}
