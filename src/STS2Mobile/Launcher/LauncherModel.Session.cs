using System;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal bool ConnectionResolved => _connectionResolved;
    internal bool AwaitingCode => _steamSession.AwaitingCode;
    internal string AccountName => _credentialStore.AccountName;
    internal string FailReason => _failReason;
    internal SessionState SessionState => _sessionState;

    internal event Action<SessionState> SessionStateChanged;
    internal event Action<string> LogReceived;
    internal event Action<bool> CodeNeeded;

    internal void MarkConnectionResolved()
    {
        _connectionResolved = true;
    }

    private bool IsLoggedIn => _sessionState == SessionState.LoggedIn;

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
