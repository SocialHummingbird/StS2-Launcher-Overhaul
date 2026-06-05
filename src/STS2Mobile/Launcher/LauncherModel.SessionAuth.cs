using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private readonly struct ConnectionAttemptResult
    {
        private ConnectionAttemptResult(
            string? failure,
            bool isCurrentAttempt
        )
        {
            Failure = failure;
            IsCurrentAttempt = isCurrentAttempt;
        }

        private string? Failure { get; }
        private bool IsCurrentAttempt { get; }
        private bool Succeeded => Failure == null;
        private SessionState State => Succeeded
            ? SessionState.LoggedIn
            : SessionState.Failed;

        private void LogIfStaleFailure()
        {
            if (!IsCurrentAttempt && Failure != null)
                PatchHelper.Log($"[Launcher] Ignored stale session failure: {Failure}");
        }

        internal static ConnectionAttemptResult Create(
            string? failure,
            bool isCurrentAttempt
        )
            => new(failure, isCurrentAttempt);

        internal void ApplyTo(LauncherModel model)
        {
            if (!IsCurrentAttempt)
            {
                LogIfStaleFailure();
                return;
            }

            model._connectionResolved = Succeeded;
            model.SetSessionState(State, Failure);
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
        ApplyConnectionAttemptResult(
            CreateConnectionAttemptResult(
                attemptId,
                await run(attemptId)
            )
        );
    }

    private ConnectionAttemptResult CreateConnectionAttemptResult(
        int attemptId,
        string? failure
    )
        => ConnectionAttemptResult.Create(
            failure,
            IsCurrentSessionAttempt(attemptId)
        );

    private void ApplyConnectionAttemptResult(ConnectionAttemptResult result)
        => result.ApplyTo(this);

    private void BeginOwnershipVerification(int attemptId)
    {
        if (!IsCurrentSessionAttempt(attemptId))
            return;

        SetSessionState(SessionState.VerifyingOwnership);
    }

}
