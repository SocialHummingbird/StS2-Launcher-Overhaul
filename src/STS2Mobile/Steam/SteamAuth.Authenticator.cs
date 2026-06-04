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

    private enum AuthCodeAttempt
    {
        Initial,
        RetryAfterIncorrectCode,
    }

    private readonly struct AuthCodePrompt
    {
        private AuthCodePrompt(
            AuthCodeAttempt attempt,
            string retryMessage,
            string initialMessage
        )
        {
            Attempt = attempt;
            RetryMessage = retryMessage;
            InitialMessage = initialMessage;
        }

        private AuthCodeAttempt Attempt { get; }
        private string RetryMessage { get; }
        private string InitialMessage { get; }

        internal string LogMessage
            => IsRetry ? RetryMessage : InitialMessage;

        internal Task<string> RequestCodeAsync(Func<bool, Task<string>> codeProvider)
            => codeProvider(IsRetry);

        private bool IsRetry => Attempt == AuthCodeAttempt.RetryAfterIncorrectCode;

        internal static AuthCodeAttempt FromSteamRetry(bool previousCodeWasIncorrect)
            => previousCodeWasIncorrect
                ? AuthCodeAttempt.RetryAfterIncorrectCode
                : AuthCodeAttempt.Initial;

        internal static AuthCodePrompt DeviceCode(bool previousCodeWasIncorrect)
            => new(
                FromSteamRetry(previousCodeWasIncorrect),
                "Previous 2FA code was incorrect, requesting new code",
                "Steam Guard 2FA code required"
            );

        internal static AuthCodePrompt EmailCode(
            string email,
            bool previousCodeWasIncorrect
        )
            => new(
                FromSteamRetry(previousCodeWasIncorrect),
                "Previous email code was incorrect, requesting new code",
                $"Steam Guard email code sent to {email}"
            );
    }

    private readonly struct AuthReconnectMonitor
    {
        private AuthReconnectMonitor(Func<bool> shouldReconnect, string retryMessage)
        {
            ShouldReconnect = shouldReconnect;
            RetryMessage = retryMessage;
        }

        private Func<bool> ShouldReconnect { get; }
        private string RetryMessage { get; }

        internal bool ShouldReconnectNow()
            => ShouldReconnect();

        internal Task MaintainAsync(Func<string, Task> reconnectAsync)
            => reconnectAsync(RetryMessage);

        internal static AuthReconnectMonitor WaitingForCode(SteamAuth auth)
            => new(() => auth.NeedsAuthReconnect, AuthCodeReconnectRetryMessage);

        internal static AuthReconnectMonitor PollingAuthResult(SteamAuth auth)
            => new(
                () => auth.ShouldReconnectWhilePolling,
                AuthPollingReconnectRetryMessage
            );
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
            var code = await WaitForTaskAndMaintainAuthConnectionAsync(
                prompt.RequestCodeAsync(_codeProvider),
                AuthReconnectMonitor.WaitingForCode(this)
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
        AuthReconnectMonitor reconnect
    )
    {
        while (true)
        {
            if (task.IsCompleted)
                return await task;

            if (reconnect.ShouldReconnectNow())
            {
                await reconnect.MaintainAsync(MaintainAuthConnectionAsync);
            }

            await Task.WhenAny(task, Task.Delay(AuthReconnectPollDelayMs));
        }
    }

    private async Task MaintainAuthConnectionAsync(string reconnectRetryMessage)
    {
        if (!await TryReconnectForAuthAsync())
            Log(reconnectRetryMessage);
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
