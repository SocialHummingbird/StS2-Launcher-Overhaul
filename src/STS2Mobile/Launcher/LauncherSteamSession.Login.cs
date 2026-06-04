using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal sealed class LoginRequest
    {
        private LoginRequest(
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

        private string Username { get; }
        private string Password { get; }
        private Action<string> LogMessage { get; }
        private Action<bool> CodeNeeded { get; }
        private Action VerifyingOwnership { get; }

        internal static LoginRequest FromCredentials(
            string username,
            string password,
            Action<string> logMessage,
            Action<bool> codeNeeded,
            Action verifyingOwnership
        )
            => new(
                username,
                password,
                logMessage,
                codeNeeded,
                verifyingOwnership
            );

        internal void AttachLog(SteamAuth auth)
            => auth.LogMessage += msg => LogMessage?.Invoke(msg);

        internal Task<SteamAuth.LoginCredentials> LoginAsync(
            SteamAuth auth,
            SteamCredentialStore credentialStore
        )
            => auth.LoginWithCredentialsAsync(
                Username,
                Password,
                credentialStore.GuardDataOrEmpty()
            );

        internal Task<string> RequestCodeAsync(
            LauncherSteamSession session,
            bool wasIncorrect
        )
            => session.RequestSteamGuardCodeAsync(wasIncorrect, CodeNeeded);

        internal void NotifyVerifyingOwnership()
            => VerifyingOwnership();
    }

    internal async Task<string?> LoginAndVerifyAsync(LoginRequest request)
    {
        try
        {
            ResetAuth();
            var auth = new SteamAuth(wasIncorrect =>
                request.RequestCodeAsync(this, wasIncorrect));
            _auth = auth;
            request.AttachLog(auth);

            var result = await request.LoginAsync(auth, _credentialStore);
            SaveLoginCredentials(result);
            ResetAuth();
            return await UseConnectionAndVerifyOwnershipAsync(
                result.CreateConnection(),
                request.NotifyVerifyingOwnership
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
