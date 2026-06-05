using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int AuthReconnectPollDelayMs = 250;
    private const string AuthCodeReconnectRetryMessage =
        "Steam auth reconnect did not complete yet; will retry while waiting for code";
    private const string AuthPollingReconnectRetryMessage =
        "Steam auth reconnect did not complete yet; will retry while polling auth result";

    private readonly struct AuthCodePrompt
    {
        private AuthCodePrompt(
            bool previousCodeWasIncorrect,
            string retryMessage,
            string initialMessage
        )
        {
            PreviousCodeWasIncorrect = previousCodeWasIncorrect;
            RetryMessage = retryMessage;
            InitialMessage = initialMessage;
        }

        private bool PreviousCodeWasIncorrect { get; }
        private string RetryMessage { get; }
        private string InitialMessage { get; }

        internal string LogMessage
            => PreviousCodeWasIncorrect ? RetryMessage : InitialMessage;

        internal Task<string> RequestCodeAsync(Func<bool, Task<string>> codeProvider)
            => codeProvider(PreviousCodeWasIncorrect);

        internal static AuthCodePrompt DeviceCode(bool previousCodeWasIncorrect)
            => new(
                previousCodeWasIncorrect,
                "Previous 2FA code was incorrect, requesting new code",
                "Steam Guard 2FA code required"
            );

        internal static AuthCodePrompt EmailCode(
            string email,
            bool previousCodeWasIncorrect
        )
            => new(
                previousCodeWasIncorrect,
                "Previous email code was incorrect, requesting new code",
                $"Steam Guard email code sent to {email}"
            );
    }

    private readonly struct AuthConnectionWatch<T>
    {
        internal AuthConnectionWatch(
            Task<T> pendingTask,
            Func<bool> shouldReconnect,
            string reconnectRetryMessage
        )
        {
            PendingTask = pendingTask;
            ShouldReconnect = shouldReconnect;
            ReconnectRetryMessage = reconnectRetryMessage;
        }

        private Task<T> PendingTask { get; }
        private Func<bool> ShouldReconnect { get; }
        private string ReconnectRetryMessage { get; }

        internal async Task<T> WaitAsync(SteamAuth owner)
        {
            while (true)
            {
                if (PendingTask.IsCompleted)
                    return await PendingTask;

                if (ShouldReconnect())
                    await MaintainConnectionAsync(owner);

                await Task.WhenAny(
                    PendingTask,
                    Task.Delay(AuthReconnectPollDelayMs)
                );
            }
        }

        private async Task MaintainConnectionAsync(SteamAuth owner)
        {
            if (!await owner.TryReconnectForAuthAsync())
                owner.Log(ReconnectRetryMessage);
        }
    }

    Task<string> IAuthenticator.GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        => RequestCodeAsync(AuthCodePrompt.DeviceCode(previousCodeWasIncorrect));

    Task<string> IAuthenticator.GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        => RequestCodeAsync(AuthCodePrompt.EmailCode(email, previousCodeWasIncorrect));

    Task<bool> IAuthenticator.AcceptDeviceConfirmationAsync()
    {
        Log("Steam mobile app confirmation requested; using Steam Guard code entry");
        return Task.FromResult(false);
    }

    private async Task<string> RequestCodeAsync(AuthCodePrompt prompt)
    {
        Log(prompt.LogMessage);

        _waitingForAuthCode = true;
        try
        {
            var code = await new AuthConnectionWatch<string>(
                prompt.RequestCodeAsync(_codeProvider),
                () => NeedsAuthReconnect,
                AuthCodeReconnectRetryMessage
            ).WaitAsync(this);
            await ReconnectBeforeCodeSubmitAsync();
            return code;
        }
        finally
        {
            _waitingForAuthCode = false;
        }
    }

    private async Task ReconnectBeforeCodeSubmitAsync()
    {
        if (!NeedsAuthReconnect)
            return;

        await ReconnectForAuthAsync();
    }

    private void ContinueWebApiAuthWithoutPersistentConnection(string message)
    {
        LogAndroidAuthConnectionLossOnce();
        Log(message);
    }

    private bool NeedsAuthReconnect
        => RequiresPersistentAuthConnection && (_needsReconnectForAuth || !_connectedGate.IsSet);

    private void LogAndroidAuthConnectionLossOnce()
    {
        if (_androidAuthConnectionLossLogged)
            return;

        _androidAuthConnectionLossLogged = true;
        Log("Steam CM connection unavailable during Android WebAPI auth; continuing credential flow");
    }
}
