using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal async Task ConnectAsync()
    {
        var attemptId = BeginSessionAttempt(SessionState.Connecting);

        var error = await _steamSession.ConnectSavedCredentialsAndVerifyAsync(
            () => BeginOwnershipVerification(attemptId)
        );
        CompleteConnectionAttempt(attemptId, error);
    }

    // Performs interactive login, then verifies ownership.
    internal async Task LoginAsync(string username, string password)
    {
        var attemptId = BeginSessionAttempt(SessionState.Authenticating);

        var error = await _steamSession.LoginAndVerifyAsync(
            username,
            password,
            RaiseLogReceived,
            RaiseCodeNeeded,
            () => BeginOwnershipVerification(attemptId)
        );
        CompleteConnectionAttempt(attemptId, error);
    }

    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);

    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.Connection != null)
            return;

        var attemptId = BeginSessionAttempt(SessionState.Connecting);

        var error = await _steamSession.EnsureConnectedAsync();
        CompleteConnectionAttempt(attemptId, error);
    }

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;

        SetSessionState(SessionState.VerifyingOwnership);
    }

    private void CompleteConnectionAttempt(int attemptId, string error)
    {
        if (!IsCurrentSessionAttempt(attemptId))
        {
            if (error != null)
                PatchHelper.Log($"[Launcher] Ignored stale session failure: {error}");
            return;
        }

        if (error == null)
        {
            _connectionResolved = true;
            SetSessionState(SessionState.LoggedIn);
            return;
        }

        _connectionResolved = false;
        SetSessionState(SessionState.Failed, error);
    }
}
