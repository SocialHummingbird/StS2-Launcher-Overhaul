using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int AuthReconnectPollDelayMs = 250;

    private readonly struct AuthCodePrompt
    {
        internal AuthCodePrompt(
            bool previousCodeWasIncorrect,
            string retryMessage,
            string initialMessage
        )
        {
            PreviousCodeWasIncorrect = previousCodeWasIncorrect;
            RetryMessage = retryMessage;
            InitialMessage = initialMessage;
        }

        internal bool PreviousCodeWasIncorrect { get; }
        private string RetryMessage { get; }
        private string InitialMessage { get; }

        internal string Message
            => PreviousCodeWasIncorrect ? RetryMessage : InitialMessage;
    }

    private readonly struct AuthReconnectMonitor
    {
        internal AuthReconnectMonitor(Func<bool> shouldReconnect, string retryMessage)
        {
            ShouldReconnect = shouldReconnect;
            RetryMessage = retryMessage;
        }

        private Func<bool> ShouldReconnect { get; }
        internal string RetryMessage { get; }

        internal bool ShouldReconnectNow()
            => ShouldReconnect();
    }

    Task<string> IAuthenticator.GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        => RequestCodeAsync(new AuthCodePrompt(
            previousCodeWasIncorrect,
            "Previous 2FA code was incorrect, requesting new code",
            "Steam Guard 2FA code required"
        ));

    Task<string> IAuthenticator.GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        => RequestCodeAsync(new AuthCodePrompt(
            previousCodeWasIncorrect,
            "Previous email code was incorrect, requesting new code",
            $"Steam Guard email code sent to {email}"
        ));

    Task<bool> IAuthenticator.AcceptDeviceConfirmationAsync()
    {
        Log("Steam mobile app confirmation requested; using Steam Guard code entry");
        return Task.FromResult(false);
    }

    private async Task<string> RequestCodeAsync(AuthCodePrompt prompt)
    {
        Log(prompt.Message);

        _waitingForAuthCode = true;
        try
        {
            var code = await WaitForTaskAndMaintainAuthConnectionAsync(
                _codeProvider(prompt.PreviousCodeWasIncorrect),
                new AuthReconnectMonitor(
                    () => NeedsAuthReconnect,
                    "Steam auth reconnect did not complete yet; will retry while waiting for code"
                )
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
                await MaintainAuthConnectionAsync(reconnect.RetryMessage);
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
