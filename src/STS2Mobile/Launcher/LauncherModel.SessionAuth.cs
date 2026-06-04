using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private readonly struct ConnectionAttemptResult
    {
        private enum AttemptState
        {
            Succeeded,
            Failed,
        }

        private ConnectionAttemptResult(AttemptState state, string failReason)
        {
            State = state;
            FailReason = failReason;
        }

        private AttemptState State { get; }
        private string FailReason { get; }
        private bool Succeeded => State == AttemptState.Succeeded;

        internal static ConnectionAttemptResult FromFailureText(string? error)
            => error == null
                ? new ConnectionAttemptResult(AttemptState.Succeeded, string.Empty)
                : new ConnectionAttemptResult(AttemptState.Failed, error);

        internal void Apply(LauncherModel model)
        {
            model._connectionResolved = Succeeded;
            model.SetSessionState(
                Succeeded ? SessionState.LoggedIn : SessionState.Failed,
                Succeeded ? null : FailReason
            );
        }

        internal void LogIfStale()
        {
            if (!Succeeded)
                PatchHelper.Log($"[Launcher] Ignored stale session failure: {FailReason}");
        }
    }

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
                new LauncherSteamSession.LoginRequest(
                    username,
                    password,
                    RaiseLogReceived,
                    RaiseCodeNeeded,
                    () => BeginOwnershipVerification(attemptId)
                )
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
        var result = ConnectionAttemptResult.FromFailureText(await run(attemptId));
        ApplyConnectionAttemptResult(attemptId, result);
    }

    private void ApplyConnectionAttemptResult(
        int attemptId,
        ConnectionAttemptResult result
    )
    {
        if (!IsCurrentSessionAttempt(attemptId))
        {
            result.LogIfStale();
            return;
        }

        result.Apply(this);
    }

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;

        SetSessionState(SessionState.VerifyingOwnership);
    }

}
