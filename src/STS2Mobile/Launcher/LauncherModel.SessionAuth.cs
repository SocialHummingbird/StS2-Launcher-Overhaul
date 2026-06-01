using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal async Task ConnectAsync()
        => await RunConnectionAttemptAsync(
            SessionState.Connecting,
            attemptId => _steamSession.ConnectSavedCredentialsAndVerifyAsync(
                () => BeginOwnershipVerification(attemptId)
            )
        );

    // Performs interactive login, then verifies ownership.
    internal async Task LoginAsync(string username, string password)
        => await RunConnectionAttemptAsync(
            SessionState.Authenticating,
            attemptId => _steamSession.LoginAndVerifyAsync(
                username,
                password,
                RaiseLogReceived,
                RaiseCodeNeeded,
                () => BeginOwnershipVerification(attemptId)
            )
        );

    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);

    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.Connection != null)
            return;

        await RunConnectionAttemptAsync(
            SessionState.Connecting,
            _ => _steamSession.EnsureConnectedAsync()
        );
    }

    private async Task RunConnectionAttemptAsync(
        SessionState state,
        Func<int, Task<string>> run
    )
    {
        var attemptId = BeginSessionAttempt(state);
        var error = await run(attemptId);
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
