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

    internal bool ConnectionResolved => _connectionResolved;
    internal bool AwaitingCode => _steamSession.AwaitingCode;
    internal int SessionAttemptId => Volatile.Read(ref _sessionAttemptId);
    internal string AccountName => _credentialStore.AccountName;
    internal string FailReason => _failReason;
    internal SessionState CurrentSessionState => _sessionState;

    internal event Action<SessionState> SessionStateChanged;
    internal event Action<string> LogReceived;
    internal event Action<bool> CodeNeeded;

    internal void MarkConnectionResolved()
    {
        _connectionResolved = true;
    }

    private bool IsLoggedIn => _sessionState == SessionState.LoggedIn;

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
