using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int AuthReconnectPollDelayMs = 250;

    Task<string> IAuthenticator.GetDeviceCodeAsync(bool previousCodeWasIncorrect)
    {
        return RequestCodeAsync(
            previousCodeWasIncorrect,
            "Previous 2FA code was incorrect, requesting new code",
            "Steam Guard 2FA code required"
        );
    }

    Task<string> IAuthenticator.GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
    {
        return RequestCodeAsync(
            previousCodeWasIncorrect,
            "Previous email code was incorrect, requesting new code",
            $"Steam Guard email code sent to {email}"
        );
    }

    Task<bool> IAuthenticator.AcceptDeviceConfirmationAsync()
    {
        Log("Steam mobile app confirmation requested; using Steam Guard code entry");
        return Task.FromResult(false);
    }

    private async Task<string> RequestCodeAsync(
        bool previousCodeWasIncorrect,
        string retryMessage,
        string initialMessage
    )
    {
        Log(previousCodeWasIncorrect ? retryMessage : initialMessage);

        return await WaitForCodeAndMaintainAuthConnectionAsync(
            _codeProvider(previousCodeWasIncorrect)
        );
    }

    private async Task<string> WaitForCodeAndMaintainAuthConnectionAsync(Task<string> codeTask)
    {
        while (true)
        {
            if (codeTask.IsCompleted)
            {
                var code = await codeTask;
                await ReconnectBeforeCodeSubmitAsync();
                return code;
            }

            if (NeedsAuthReconnect && !await TryReconnectForAuthAsync())
                Log("Steam auth reconnect did not complete yet; will retry while waiting for code");

            await Task.WhenAny(codeTask, Task.Delay(AuthReconnectPollDelayMs));
        }
    }

    private async Task ReconnectBeforeCodeSubmitAsync()
    {
        if (NeedsAuthReconnect)
            await ReconnectForAuthAsync();
    }

    private bool NeedsAuthReconnect => _needsReconnectForAuth || !_connectedGate.IsSet;
}
