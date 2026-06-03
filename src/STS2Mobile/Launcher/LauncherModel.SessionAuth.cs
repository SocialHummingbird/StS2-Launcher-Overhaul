using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private readonly struct ConnectionAttemptResult
    {
        private ConnectionAttemptResult(int attemptId, string? error)
        {
            AttemptId = attemptId;
            Error = error;
        }

        private int AttemptId { get; }
        private string? Error { get; }
        private bool Succeeded => Error == null;

        internal static ConnectionAttemptResult Create(int attemptId, string? error)
            => new(attemptId, error);

        internal void Apply(LauncherModel model)
        {
            if (!model.IsCurrentSessionAttempt(AttemptId))
            {
                LogStaleFailure();
                return;
            }

            if (Succeeded)
            {
                model.MarkConnectionSucceeded();
                return;
            }

            model.MarkConnectionFailed(Error);
        }

        private void LogStaleFailure()
        {
            if (Error != null)
                PatchHelper.Log($"[Launcher] Ignored stale session failure: {Error}");
        }
    }

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
        if (IsLoggedIn && _steamSession.TryGetConnection(out _))
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
        ConnectionAttemptResult.Create(attemptId, error).Apply(this);
    }

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;

        SetSessionState(SessionState.VerifyingOwnership);
    }

    private void MarkConnectionSucceeded()
    {
        _connectionResolved = true;
        SetSessionState(SessionState.LoggedIn);
    }

    private void MarkConnectionFailed(string error)
    {
        _connectionResolved = false;
        SetSessionState(SessionState.Failed, error);
    }
}
