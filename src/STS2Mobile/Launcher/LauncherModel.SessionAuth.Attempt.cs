using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
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
