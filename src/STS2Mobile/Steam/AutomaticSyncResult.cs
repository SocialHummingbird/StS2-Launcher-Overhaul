#nullable enable

namespace STS2Mobile.Steam;

internal enum AutomaticSyncSourceChoice
{
    Local,
    Steam,
}

internal enum AutomaticSyncOutcome
{
    Synchronized,
    SourceChoiceRequired,
    Conflict,
    GameSessionPrepared,
    PendingRecoveryRequired,
}

internal readonly record struct AutomaticSyncResult(
    AutomaticSyncOutcome Outcome,
    string Message
)
{
    internal bool CanStartGame
        => Outcome == AutomaticSyncOutcome.GameSessionPrepared;

    internal bool HasConflict
        => Outcome == AutomaticSyncOutcome.Conflict;
}
