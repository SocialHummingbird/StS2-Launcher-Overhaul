using System;
using System.Threading;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Represents the current stage of the launcher's Steam connection and
    // authentication flow. Drives the launcher UI state machine.
    internal enum SessionState
    {
        Disconnected,
        Connecting,
        Authenticating,
        VerifyingOwnership,
        LoggedIn,
        Failed,
    }

    private bool ConnectionResolved => _connectionResolved;
    private bool AwaitingCode => _steamSession.IsAwaitingCode();
    private int SessionAttemptId => Volatile.Read(ref _sessionAttemptId);
    private string AccountName => _credentialStore.AccountNameOrEmpty();
    private string FailReason => _failReason;
    private SessionState CurrentSessionState => _sessionState;

    internal event Action<SessionState> SessionStateChanged;
    internal event Action<string> LogReceived;
    internal event Action<bool> CodeNeeded;

    internal void MarkConnectionResolved()
    {
        _connectionResolved = true;
    }

    internal string LoggedInStatus()
        => $"Logged in as {AccountName}";

    internal string WelcomeBackStatus()
        => $"Welcome back, {AccountName}";

    internal string FailureStatus()
        => $"Error: {FailReason}";

    internal bool IsConnectionPending()
        => !ConnectionResolved;

    internal void StartConnectionTimeout(Action<int> startTimeout)
        => startTimeout(SessionAttemptId);

    internal bool ShouldIgnoreConnectionTimeout(int sessionAttemptId)
    {
        if (SessionAttemptId != sessionAttemptId || AwaitingCode)
            return true;

        if (ConnectionResolved)
            return true;

        return !IsConnectionAttemptState(CurrentSessionState);
    }

    internal bool ShouldSuppressSessionUpdate(SessionState state, bool updateCheckRunning)
    {
        if (
            AwaitingCode
            && IsAuthenticationProgressState(state)
        )
            return true;

        if (updateCheckRunning)
            return true;

        return state == SessionState.Disconnected && ConnectionResolved;
    }

    private bool IsLoggedIn => _sessionState == SessionState.LoggedIn;

    private static bool IsConnectionAttemptState(SessionState state)
        => state
            is SessionState.Connecting
                or SessionState.Authenticating
                or SessionState.VerifyingOwnership;

    private static bool IsAuthenticationProgressState(SessionState state)
        => state
            is SessionState.Connecting
                or SessionState.Authenticating;

    private int BeginSessionAttempt(SessionState state)
    {
        var attemptId = Interlocked.Increment(ref _sessionAttemptId);
        SetSessionState(state);
        return attemptId;
    }

    private bool IsCurrentSessionAttempt(int attemptId)
        => SessionAttemptId == attemptId;

    private void SetSessionState(SessionState state, string failReason = null)
    {
        _sessionState = state;
        _failReason = failReason;
        RaiseSessionStateChanged(state);
    }

    private void RaiseSessionStateChanged(SessionState state)
        => Raise(SessionStateChanged, state, nameof(SessionStateChanged));

    private void RaiseLogReceived(string message)
        => Raise(LogReceived, message, nameof(LogReceived));

    private void RaiseCodeNeeded(bool wasIncorrect)
        => Raise(CodeNeeded, wasIncorrect, nameof(CodeNeeded));
}
