using System;
using System.Threading.Tasks;

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
            attemptId => RunLoginAttemptAsync(attemptId, username, password)
        );

    internal Task LoginWithTimeoutAsync(
        string username,
        string password,
        Action<int> startTimeout
    )
        => RunConnectionAttemptAsync(
            SessionState.Authenticating,
            attemptId =>
            {
                startTimeout(attemptId);
                return RunLoginAttemptAsync(attemptId, username, password);
            }
        );

    private Task<string?> RunLoginAttemptAsync(
        int attemptId,
        string username,
        string password
    )
        => _steamSession.LoginAndVerifyAsync(
            username,
            password,
            RaiseLogReceived,
            RaiseCodeNeeded,
            () => BeginOwnershipVerification(attemptId)
        );

    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);
}
