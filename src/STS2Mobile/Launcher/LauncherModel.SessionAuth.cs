using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal Task ConnectAsync()
        => RunConnectionAttemptAsync(
            SessionState.Connecting,
            attemptId => _steamSession.ConnectSavedCredentialsAndVerifyAsync(
                () => BeginOwnershipVerification(attemptId)
            )
        );

    // Performs interactive login, then verifies ownership.
    internal Task LoginAsync(string username, string password)
        => RunConnectionAttemptAsync(
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
        if (IsLoggedIn && _steamSession.TryGetConnection(out _))
            return;

        await RunConnectionAttemptAsync(
            SessionState.Connecting,
            _ => _steamSession.EnsureConnectedAsync()
        );
    }

    private async Task RunConnectionAttemptAsync(
        SessionState state,
        Func<int, Task<string?>> run
    )
    {
        var attemptId = BeginSessionAttempt(state);
        ApplyConnectionAttemptResult(attemptId, await run(attemptId));
    }

    private void ApplyConnectionAttemptResult(
        int attemptId,
        string? failure
    )
    {
        if (!IsCurrentSessionAttempt(attemptId))
        {
            LogStaleConnectionFailure(failure);
            return;
        }

        var succeeded = failure == null;
        _connectionResolved = succeeded;
        SetSessionState(
            succeeded ? SessionState.LoggedIn : SessionState.Failed,
            failure
        );
    }

    private static void LogStaleConnectionFailure(string? failure)
    {
        if (failure != null)
            PatchHelper.Log($"[Launcher] Ignored stale session failure: {failure}");
    }

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;

        SetSessionState(SessionState.VerifyingOwnership);
    }

}
