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
}
