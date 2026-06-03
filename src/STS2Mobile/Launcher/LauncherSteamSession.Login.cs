using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal readonly struct LoginRequest
    {
        internal LoginRequest(
            string username,
            string password,
            Action<string> logMessage,
            Action<bool> codeNeeded,
            Action verifyingOwnership
        )
        {
            Username = username;
            Password = password;
            LogMessage = logMessage;
            CodeNeeded = codeNeeded;
            VerifyingOwnership = verifyingOwnership;
        }

        internal string Username { get; }
        internal string Password { get; }
        internal Action<string> LogMessage { get; }
        internal Action<bool> CodeNeeded { get; }
        internal Action VerifyingOwnership { get; }
    }

    internal async Task<string> LoginAndVerifyAsync(LoginRequest request)
    {
        try
        {
            ResetAuth();
            var auth = new SteamAuth(wasIncorrect =>
                RequestSteamGuardCodeAsync(wasIncorrect, request.CodeNeeded));
            _auth = auth;
            auth.LogMessage += msg => request.LogMessage?.Invoke(msg);

            var result = await auth.LoginWithCredentialsAsync(
                request.Username,
                request.Password,
                _credentialStore.GuardDataOrEmpty()
            );
            SaveLoginCredentials(result);
            ResetAuth();
            return await UseConnectionAndVerifyOwnershipAsync(
                result.CreateConnection(),
                request.VerifyingOwnership
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
