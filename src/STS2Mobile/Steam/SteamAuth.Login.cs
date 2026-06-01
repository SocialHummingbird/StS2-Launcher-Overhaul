using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const int CredentialAuthRetryCount = 3;

    internal async Task<LoginCredentials> LoginWithCredentialsAsync(
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
                    return await AuthenticateCredentialsOnceAsync(username, password, guardData);
                }
                catch (Exception ex) when (CanRetryAuthAfterTransientFailure(ex, attempt))
                {
                    Log($"Steam authentication interrupted ({ex.Message}); retrying auth session");
                    await PrepareForAuthRetryAsync();
                }
            }

            throw new TimeoutException("Steam authentication did not complete.");
        }
        finally
        {
            _credentialAuthStarted = false;
        }
    }

    private async Task<LoginCredentials> AuthenticateCredentialsOnceAsync(
        string username,
        string password,
        string guardData
    )
    {
        var authSession = await BeginCredentialAuthSessionAsync(
            username,
            password,
            guardData
        );

        var pollResponse = await PollForAuthResultAndMaintainConnectionAsync(authSession);
        Log($"Authentication successful for '{pollResponse.AccountName}'");

        return new LoginCredentials(
            pollResponse.AccountName,
            pollResponse.RefreshToken,
            pollResponse.NewGuardData ?? guardData
        );
    }

    private async Task<AuthPollResult> PollForAuthResultAndMaintainConnectionAsync(
        CredentialsAuthSession authSession
    )
        => await WaitForTaskAndMaintainAuthConnectionAsync(
            authSession.PollingWaitForResultAsync(),
            () => ShouldReconnectWhilePolling,
            "Steam auth reconnect did not complete yet; will retry while polling auth result"
        );

    private bool ShouldReconnectWhilePolling => NeedsAuthReconnect && !_waitingForAuthCode;

    private bool CanRetryAuthAfterTransientFailure(Exception ex, int attempt)
        => attempt < CredentialAuthRetryCount
            && (AuthConnectionWasLost || IsTransientAndroidAuthFailure(ex));

    private bool AuthConnectionWasLost
        => RequiresPersistentAuthConnection && (_needsReconnectForAuth || !_connectedGate.IsSet);

    private async Task PrepareForAuthRetryAsync()
    {
        if (ContinueWebApiAuthWithoutPersistentConnection(
                "Retrying Android WebAPI credential auth without CM reconnect"
            ))
        {
            return;
        }

        await ReconnectForAuthAsync();
    }

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
