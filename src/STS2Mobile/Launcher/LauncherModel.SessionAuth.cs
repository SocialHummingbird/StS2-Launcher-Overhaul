using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;
internal partial class LauncherModel
{
    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal Task ConnectAsync() => RunConnectionAttemptAsync(SessionState.Connecting, attemptId => _steamSession.ConnectSavedCredentialsAndVerifyAsync(() => BeginOwnershipVerification(attemptId)));
    // Performs interactive login, then verifies ownership.
    internal Task LoginAsync(string username, string password) => RunConnectionAttemptAsync(SessionState.Authenticating, attemptId => RunLoginAttemptAsync(attemptId, username, password));
    internal Task LoginWithTimeoutAsync(string username, string password, Action<int> startTimeout) => RunConnectionAttemptAsync(SessionState.Authenticating, attemptId =>
    {
        startTimeout(attemptId);
        return RunLoginAttemptAsync(attemptId, username, password);
    });
    private Task<string?> RunLoginAttemptAsync(int attemptId, string username, string password) => _steamSession.LoginAndVerifyAsync(username, password, RaiseLogReceived, RaiseCodeNeeded, () => BeginOwnershipVerification(attemptId));
    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);
    private async Task RunConnectionAttemptAsync(SessionState state, Func<int, Task<string?>> run)
    {
        var attemptId = BeginSessionAttempt(state);
        ApplyConnectionAttemptResult(attemptId, await run(attemptId));
    }

    private void ApplyConnectionAttemptResult(int attemptId, string? failure)
    {
        if (!IsCurrentSessionAttempt(attemptId))
        {
            if (failure != null)
                PatchHelper.Log($"[Launcher] Ignored stale session failure: {failure}");
            return;
        }

        _connectionResolved = failure == null;
        SetSessionState(failure == null ? SessionState.LoggedIn : SessionState.Failed, failure);
    }

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;
        SetSessionState(SessionState.VerifyingOwnership);
    }

    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.TryGetConnection(out _))
            return;
        await RunConnectionAttemptAsync(SessionState.Connecting, _ => _steamSession.EnsureConnectedAsync());
    }
}
