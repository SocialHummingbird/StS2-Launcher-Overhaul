using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
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
            ResetAuth();
            var auth = new SteamAuth(wasIncorrect =>
                RequestSteamGuardCodeAsync(wasIncorrect, codeNeeded));
            _auth = auth;
            auth.LogMessage += msg => logMessage?.Invoke(msg);

            var result = await auth.LoginWithCredentialsAsync(
                username,
                password,
                _credentialStore.GuardDataOrEmpty()
            );
            SaveLoginCredentials(result);
            ResetAuth();
            return await UseConnectionAndVerifyOwnershipAsync(
                result.CreateConnection(),
                verifyingOwnership
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login failed: {ex.Message}");
            ResetAuth();
            return ex.Message;
        }
    }

    private void SaveLoginCredentials(SteamAuth.LoginCredentials result)
    {
        result.SaveTo(_credentialStore);
        LauncherCloudSaveState.SaveCredentials(_credentialStore);
    }
}
