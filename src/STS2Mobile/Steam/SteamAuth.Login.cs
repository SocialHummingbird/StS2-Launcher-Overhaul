using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SteamKit2.Authentication;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;
internal sealed partial class SteamAuth
{
    private const int CredentialAuthRetryCount = 3;
    private readonly struct CredentialAuthAttempt
    {
        internal CredentialAuthAttempt(int number)
        {
            Number = number;
        }

        private int Number { get; }

        internal bool CanRetry(Exception ex, bool needsAuthReconnect, bool authDisposed) => Number < CredentialAuthRetryCount && !authDisposed && (ex is not AuthenticationException) && IsRecoverableAuthInterruption(ex, needsAuthReconnect);
    }

    private readonly struct CredentialAuthRunner
    {
        private CredentialAuthRunner(SteamAuth owner, string username, string password, string guardData)
        {
            Owner = owner;
            Username = username;
            Password = password;
            GuardData = guardData;
        }

        private SteamAuth Owner { get; }
        private string Username { get; }
        private string Password { get; }
        private string GuardData { get; }

        internal static Task<LoginCredentials> RunAsync(SteamAuth owner, string username, string password, string guardData) => new CredentialAuthRunner(owner, username, password, guardData).RunAsync();
        private async Task<LoginCredentials> RunAsync()
        {
            Owner.Log($"Authenticating as '{Username}'...");
            for (int attempt = 1; attempt <= CredentialAuthRetryCount; attempt++)
            {
                try
                {
                    return await Owner.AuthenticateCredentialsOnceAsync(Username, Password, GuardData);
                }
                catch (Exception ex)when (new CredentialAuthAttempt(attempt).CanRetry(ex, Owner.NeedsAuthReconnect, Owner._disposed))
                {
                    Owner.LogCredentialAuthRetry(ex);
                    await Owner.PrepareForAuthRetryAsync(ex);
                }
            }

            throw new TimeoutException("Steam authentication did not complete.");
        }
    }

    internal async Task<LoginCredentials> LoginWithCredentialsAsync(string username, string password, string guardData)
    {
        await EnsureConnectedForLoginAsync();
        return await RunWhileCredentialAuthStartedAsync(() => CredentialAuthRunner.RunAsync(this, username, password, guardData));
    }

    private async Task<LoginCredentials> RunWhileCredentialAuthStartedAsync(Func<Task<LoginCredentials>> runAsync)
    {
        _credentialAuthStarted = true;
        try
        {
            return await runAsync();
        }
        finally
        {
            _credentialAuthStarted = false;
        }
    }

    private void LogCredentialAuthRetry(Exception ex) => Log($"Steam authentication interrupted ({ex.Message}); retrying auth session");
    private async Task<LoginCredentials> AuthenticateCredentialsOnceAsync(string username, string password, string guardData)
    {
        var authSession = await BeginCredentialAuthSessionAsync(username, password, guardData);
        var pollResponse = await PollForAuthResultAndMaintainConnectionAsync(authSession);
        Log($"Authentication successful for '{pollResponse.AccountName}'");
        return new LoginCredentials(pollResponse.AccountName, pollResponse.RefreshToken, pollResponse.NewGuardData ?? guardData);
    }

    private Task<AuthPollResult> PollForAuthResultAndMaintainConnectionAsync(CredentialsAuthSession authSession) => new AuthConnectionWatch<AuthPollResult>(authSession.PollingWaitForResultAsync(), () => ShouldReconnectWhilePolling, AuthPollingReconnectRetryMessage).WaitAsync(this);
    private bool ShouldReconnectWhilePolling => NeedsAuthReconnect && !_waitingForAuthCode;

    private async Task PrepareForAuthRetryAsync(Exception ex)
    {
        if (!RequiresPersistentAuthConnection)
        {
            if (IsSteamConnectionInvalidOperation(ex))
            {
                await ForceReconnectForLoginRetryAsync("Reconnecting before retrying Android credential auth after SteamClient connection failure");
                return;
            }

            if (!_connectedGate.IsSet)
            {
                Log("Reconnecting before retrying Android credential auth");
                await EnsureConnectedForLoginAsync();
                return;
            }

            ContinueWebApiAuthWithoutPersistentConnection("Retrying Android WebAPI credential auth without CM reconnect");
            return;
        }

        await ReconnectForAuthAsync();
    }

    private static bool IsTransientAndroidAuthFailure(Exception ex)
    {
        if (!OperatingSystem.IsAndroid())
            return false;
        return (ex is TimeoutException or TaskCanceledException or HttpRequestException or IOException) || IsSteamConnectionInvalidOperation(ex);
    }

    private static bool IsRecoverableAuthInterruption(Exception ex, bool needsAuthReconnect) => needsAuthReconnect || IsTransientAndroidAuthFailure(ex);
    private static bool IsSteamConnectionInvalidOperation(Exception ex) => ex is InvalidOperationException && ex.Message.IndexOf("connect", StringComparison.OrdinalIgnoreCase) >= 0;
    private async Task EnsureConnectedForLoginAsync()
    {
        if (_connectedGate.IsSet)
            return;
        if (!await ConnectWithRetriesAsync())
            throw new TimeoutException("Could not establish a Steam auth connection. Check Steam status and try again.");
    }

    private const string AndroidDeviceFriendlyName = "StS2 Launcher Android";
    private const string SteamClientWebsiteId = "Client";
    private Task<CredentialsAuthSession> BeginCredentialAuthSessionAsync(string username, string password, string guardData) => _client.Authentication.BeginAuthSessionViaCredentialsAsync(CreateAuthSessionDetails(username, password, guardData));
    private AuthSessionDetails CreateAuthSessionDetails(string username, string password, string guardData)
    {
        var details = new AuthSessionDetails
        {
            Username = username,
            Password = password,
            IsPersistentSession = true,
            GuardData = guardData,
            Authenticator = this,
        };
        if (OperatingSystem.IsAndroid())
        {
            details.DeviceFriendlyName = AndroidDeviceFriendlyName;
            details.PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_SteamClient;
            details.ClientOSType = EOSType.AndroidUnknown;
            details.WebsiteID = SteamClientWebsiteId;
        }

        return details;
    }
}
