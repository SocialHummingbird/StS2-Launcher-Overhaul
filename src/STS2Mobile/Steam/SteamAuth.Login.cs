using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int CredentialAuthRetryCount = 3;

    internal async Task<(
        string AccountName,
        string RefreshToken,
        string GuardData
    )> LoginWithCredentialsAsync(
        string username,
        string password,
        string guardData
    )
    {
        await EnsureConnectedForLoginAsync();

        _credentialAuthStarted = true;
        try
        {
            Log($"Authenticating as '{username}'...");

            for (int attempt = 1; attempt <= CredentialAuthRetryCount; attempt++)
            {
                try
                {
                    var authSession = await BeginCredentialAuthSessionAsync(
                        username,
                        password,
                        guardData
                    );

                    var pollResponse = await authSession.PollingWaitForResultAsync();
                    string newGuardData = pollResponse.NewGuardData ?? guardData;

                    Log($"Authentication successful for '{pollResponse.AccountName}'");

                    return (
                        AccountName: pollResponse.AccountName,
                        RefreshToken: pollResponse.RefreshToken,
                        GuardData: newGuardData
                    );
                }
                catch (Exception ex) when (CanRetryAuthAfterTransientFailure(ex, attempt))
                {
                    Log($"Steam authentication interrupted ({ex.Message}); retrying auth session");
                    await ReconnectForAuthAsync();
                }
            }

            throw new TimeoutException("Steam authentication did not complete.");
        }
        finally
        {
            _credentialAuthStarted = false;
        }
    }

    private bool CanRetryAuthAfterTransientFailure(Exception ex, int attempt)
        => attempt < CredentialAuthRetryCount
            && (AuthConnectionWasLost || IsTransientAndroidAuthFailure(ex));

    private bool AuthConnectionWasLost => _needsReconnectForAuth || !_connectedGate.IsSet;

    private static bool IsTransientAndroidAuthFailure(Exception ex)
    {
        if (!OperatingSystem.IsAndroid() || ex is AuthenticationException)
            return false;

        return (ex is TimeoutException
            or TaskCanceledException
            or HttpRequestException
            or IOException)
            || IsSteamConnectionInvalidOperation(ex);
    }

    private static bool IsSteamConnectionInvalidOperation(Exception ex)
        => ex is InvalidOperationException
            && ex.Message.IndexOf("connect", StringComparison.OrdinalIgnoreCase) >= 0;
}
