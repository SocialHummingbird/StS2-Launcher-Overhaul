using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    internal async Task<AuthResult> LoginWithCredentialsAsync(
        string username,
        string password,
        string guardData
    )
    {
        await EnsureConnectedForLoginAsync();

        Log($"Authenticating as '{username}'...");

        _forceSteamGuardCodeEntry = false;
        bool retriedAfterMobileConfirmationFailure = false;

        while (true)
        {
            _mobileConfirmationRequested = false;

            var authSession = await BeginCredentialAuthSessionAsync(
                username,
                password,
                guardData
            );

            try
            {
                var pollResponse = await authSession.PollingWaitForResultAsync();
                string newGuardData = pollResponse.NewGuardData ?? guardData;

                Log($"Authentication successful for '{pollResponse.AccountName}'");

                return new AuthResult(
                    pollResponse.AccountName,
                    pollResponse.RefreshToken,
                    newGuardData
                );
            }
            catch (Exception ex) when (CanFallbackToSteamGuardCode(ex, retriedAfterMobileConfirmationFailure))
            {
                retriedAfterMobileConfirmationFailure = true;
                await FallbackToSteamGuardCodeAsync(MobileConfirmationFailureMessage(ex));
            }
        }
    }

    private bool CanFallbackToSteamGuardCode(Exception ex, bool alreadyRetried)
        => _mobileConfirmationRequested
            && !alreadyRetried
            && ex is AsyncJobFailedException or TaskCanceledException;

    private static string MobileConfirmationFailureMessage(Exception ex)
        => ex is TaskCanceledException
            ? "Steam mobile app confirmation was canceled; falling back to Steam Guard code entry"
            : "Steam mobile app confirmation did not complete; falling back to Steam Guard code entry";

    private async Task FallbackToSteamGuardCodeAsync(string message)
    {
        _forceSteamGuardCodeEntry = true;
        Log(message);

        if (_needsReconnectForAuth || !_connectedGate.IsSet)
            await ReconnectForAuthAsync();
    }
}
