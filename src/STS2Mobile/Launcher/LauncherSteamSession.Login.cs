using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal async Task<string> LoginAndVerifyAsync(
        string username,
        string password,
        Action<string> logMessage,
        Action<bool> codeNeeded,
        Action verifyingOwnership
    )
    {
        try
        {
            ResetAuth();
            var auth = new SteamAuth(wasIncorrect =>
                RequestSteamGuardCodeAsync(wasIncorrect, codeNeeded));
            _auth = auth;
            auth.LogMessage += msg => logMessage?.Invoke(msg);
            auth.Connect();

            var result = await auth.LoginWithCredentialsAsync(
                username,
                password,
                _credentialStore.GuardData
            );
            SaveLoginCredentials(result);
            _connection = new SteamConnection(result.AccountName, result.RefreshToken);
            ResetAuth();
            return await VerifyOwnershipForSessionAsync(_connection, verifyingOwnership);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login failed: {ex.Message}");
            ResetAuth();
            return ex.Message;
        }
    }

    private void SaveLoginCredentials((string AccountName, string RefreshToken, string GuardData) result)
    {
        _credentialStore.Save(result.AccountName, result.RefreshToken, result.GuardData);
        LauncherCloudSaveState.SaveCredentials(_credentialStore);
    }
}
