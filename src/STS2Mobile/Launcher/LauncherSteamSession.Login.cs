using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal async Task<string?> LoginAndVerifyAsync(
        string username,
        string password,
        Action<string> logMessage,
        Action<bool> codeNeeded,
        Action verifyingOwnership
    )
    {
        try
        {
            var result = await LoginWithCredentialsAsync(
                username,
                password,
                logMessage,
                codeNeeded
            );
            var failure = await UseConnectionAndVerifyOwnershipAsync(
                result.CreateConnection(),
                verifyingOwnership,
                result.AccountName,
                () => Task.FromResult(SaveLoginCredentials(result)),
                saveOwnershipMarker: false
            );
            return failure;
        }
        catch (Exception ex)
        {
            ResetAuth();
            return SessionFailure("Login failed", ex);
        }
    }

    private async Task<SteamAuth.LoginCredentials> LoginWithCredentialsAsync(
        string username,
        string password,
        Action<string> logMessage,
        Action<bool> codeNeeded
    )
    {
        ResetAuth();
        var auth = CreateAuthSession(logMessage, codeNeeded);
        try
        {
            return await auth.LoginWithCredentialsAsync(
                username,
                password,
                _credentialStore.GuardDataOrEmpty()
            );
        }
        finally
        {
            ResetAuth();
        }
    }

    private SteamAuth CreateAuthSession(
        Action<string> logMessage,
        Action<bool> codeNeeded
    )
    {
        var auth = new SteamAuth(wasIncorrect =>
            RequestSteamGuardCodeAsync(wasIncorrect, codeNeeded));
        _auth = auth;
        auth.LogMessage += msg => logMessage?.Invoke(msg);
        return auth;
    }

    private string? SaveLoginCredentials(SteamAuth.LoginCredentials result)
    {
        if (!result.SaveTo(_credentialStore))
            return "Could not save Steam credentials. Login was not completed.";

        if (!LauncherCloudSaveState.SaveCredentials(_credentialStore))
            return "Could not update cloud save credentials. Login was not completed.";

        if (!SaveOwnershipMarker(result.AccountName))
            return "Could not save ownership marker. Login was not completed.";

        return null;
    }
}
