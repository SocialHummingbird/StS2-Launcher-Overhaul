using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private sealed class AuthAuthenticator : IAuthenticator
    {
        private readonly SteamAuth _auth;

        private AuthAuthenticator(SteamAuth auth) => _auth = auth;

        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            return _auth.RequestCodeAsync(
                previousCodeWasIncorrect,
                "Previous 2FA code was incorrect, requesting new code",
                "Steam Guard 2FA code required"
            );
        }

        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            return _auth.RequestCodeAsync(
                previousCodeWasIncorrect,
                "Previous email code was incorrect, requesting new code",
                $"Steam Guard email code sent to {email}"
            );
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            if (_auth._forceSteamGuardCodeEntry)
            {
                _auth.Log("Steam mobile app confirmation requested; using Steam Guard code entry");
                return Task.FromResult(false);
            }

            _auth._mobileConfirmationRequested = true;
            _auth.Log("Steam mobile app confirmation requested; waiting for approval");
            return Task.FromResult(true);
        }
    }

    private async Task<string> RequestCodeAsync(
        bool previousCodeWasIncorrect,
        string retryMessage,
        string initialMessage
    )
    {
        Log(previousCodeWasIncorrect ? retryMessage : initialMessage);

        var code = await _codeProvider(previousCodeWasIncorrect);
        if (_needsReconnectForAuth)
            await ReconnectForAuthAsync();

        return code;
    }
}
