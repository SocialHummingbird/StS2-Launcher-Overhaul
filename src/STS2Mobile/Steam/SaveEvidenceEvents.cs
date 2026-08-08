#nullable enable

using System;
using System.IO;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal enum AutomaticSyncEvidenceOperation
{
    Recover,
    Reconcile,
    BeginGame,
}

internal enum AutomaticSyncEvidenceDetail
{
    Unspecified,
    Verified,
    NoPendingWork,
    SourceChoiceRequired,
    LocalAndRemoteDiverged,
    AccountMismatch,
    NamespaceMismatch,
    BranchMismatch,
    ModSetMismatch,
    ContextMissing,
    ContextUnreadable,
    IndependentChange,
    SessionPrepared,
    PendingRecovery,
    CommitRejected,
    RemoteReadBackMismatch,
    OperationFailed,
}

internal sealed class CloudFileCommitRejectedException :
    InvalidOperationException
{
    internal CloudFileCommitRejectedException(string message)
        : base(message)
    {
    }
}

internal sealed class SaveTransferReadBackMismatchException :
    IOException
{
    internal SaveTransferReadBackMismatchException(
        string message,
        bool remote
    ) : base(message)
    {
        Remote = remote;
    }

    internal bool Remote { get; }
}

internal static class SaveEvidenceEvents
{
    internal const string Marker = "STS2_SAVE_EVENT ";

    internal static void AutomaticSyncTerminal(
        AutomaticSyncEvidenceOperation operation,
        AutomaticSyncOutcome outcome,
        AutomaticSyncEvidenceDetail detail,
        string contextSha256,
        bool remoteVerified
    )
        => WriteAutomaticSyncTerminal(
            OperationName(operation),
            OutcomeName(outcome),
            DetailName(detail),
            contextSha256,
            remoteVerified
        );

    internal static void AutomaticSyncFailure(
        AutomaticSyncEvidenceOperation operation,
        Exception exception,
        string contextSha256
    )
    {
        ArgumentNullException.ThrowIfNull(exception);
        var detail = exception switch
        {
            CloudFileCommitRejectedException
                => AutomaticSyncEvidenceDetail.CommitRejected,
            SaveTransferReadBackMismatchException { Remote: true }
                => AutomaticSyncEvidenceDetail.RemoteReadBackMismatch,
            _ => AutomaticSyncEvidenceDetail.OperationFailed,
        };
        WriteAutomaticSyncTerminal(
            OperationName(operation),
            "failed",
            DetailName(detail),
            contextSha256,
            remoteVerified: false
        );
    }

    internal static void RecoveryTerminal(string operation)
    {
        if (operation is not ("restore" or "undo"))
            throw new ArgumentOutOfRangeException(nameof(operation));

        PatchHelper.Log(
            Marker
                + JsonSerializer.Serialize(new
                {
                    Event = "save-recovery-terminal",
                    Version = 1,
                    Operation = operation,
                    Outcome = "completed",
                    Detail = "byte-verified-local-only",
                })
        );
    }

    internal static AutomaticSyncEvidenceDetail ContextMismatch(
        SaveContext expected,
        SaveContext actual
    )
    {
        if (expected.SteamId64 != actual.SteamId64)
            return AutomaticSyncEvidenceDetail.AccountMismatch;
        if (expected.Namespace != actual.Namespace)
            return AutomaticSyncEvidenceDetail.NamespaceMismatch;
        if (!string.Equals(
                expected.RuntimeIdentity,
                actual.RuntimeIdentity,
                StringComparison.Ordinal
            ))
        {
            return AutomaticSyncEvidenceDetail.BranchMismatch;
        }
        if (!string.Equals(
                expected.ModSetFingerprint,
                actual.ModSetFingerprint,
                StringComparison.Ordinal
            ))
        {
            return AutomaticSyncEvidenceDetail.ModSetMismatch;
        }
        return AutomaticSyncEvidenceDetail.Unspecified;
    }

    internal static string ContextSha256(SaveContext context)
        => AutomaticSyncHash.Compute(
            $"{context.SteamId64}\0{context.NamespaceName}\0"
                + $"{context.RuntimeIdentity}\0{context.ModSetFingerprint}\0"
        );

    private static void WriteAutomaticSyncTerminal(
        string operation,
        string outcome,
        string detail,
        string contextSha256,
        bool remoteVerified
    )
        => PatchHelper.Log(
            Marker
                + JsonSerializer.Serialize(new
                {
                    Event = "automatic-sync-terminal",
                    Version = 1,
                    Operation = operation,
                    Outcome = outcome,
                    Detail = detail,
                    ContextSha256 = contextSha256,
                    RemoteVerified = remoteVerified,
                })
        );

    private static string OperationName(
        AutomaticSyncEvidenceOperation operation
    )
        => operation switch
        {
            AutomaticSyncEvidenceOperation.Recover => "recover",
            AutomaticSyncEvidenceOperation.Reconcile => "reconcile",
            AutomaticSyncEvidenceOperation.BeginGame => "begin-game",
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

    private static string OutcomeName(AutomaticSyncOutcome outcome)
        => outcome switch
        {
            AutomaticSyncOutcome.Synchronized => "synchronized",
            AutomaticSyncOutcome.SourceChoiceRequired
                => "source-choice-required",
            AutomaticSyncOutcome.Conflict => "conflict",
            AutomaticSyncOutcome.GameSessionPrepared
                => "game-session-prepared",
            AutomaticSyncOutcome.PendingRecoveryRequired
                => "pending-recovery-required",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

    private static string DetailName(AutomaticSyncEvidenceDetail detail)
        => detail switch
        {
            AutomaticSyncEvidenceDetail.Unspecified => "unspecified",
            AutomaticSyncEvidenceDetail.Verified => "verified",
            AutomaticSyncEvidenceDetail.NoPendingWork => "no-pending-work",
            AutomaticSyncEvidenceDetail.SourceChoiceRequired
                => "source-choice-required",
            AutomaticSyncEvidenceDetail.LocalAndRemoteDiverged
                => "local-and-remote-diverged",
            AutomaticSyncEvidenceDetail.AccountMismatch
                => "account-mismatch",
            AutomaticSyncEvidenceDetail.NamespaceMismatch
                => "namespace-mismatch",
            AutomaticSyncEvidenceDetail.BranchMismatch => "branch-mismatch",
            AutomaticSyncEvidenceDetail.ModSetMismatch => "mod-set-mismatch",
            AutomaticSyncEvidenceDetail.ContextMissing => "context-missing",
            AutomaticSyncEvidenceDetail.ContextUnreadable
                => "context-unreadable",
            AutomaticSyncEvidenceDetail.IndependentChange
                => "independent-change",
            AutomaticSyncEvidenceDetail.SessionPrepared
                => "session-prepared",
            AutomaticSyncEvidenceDetail.PendingRecovery
                => "pending-recovery",
            AutomaticSyncEvidenceDetail.CommitRejected => "commit-rejected",
            AutomaticSyncEvidenceDetail.RemoteReadBackMismatch
                => "remote-readback-mismatch",
            AutomaticSyncEvidenceDetail.OperationFailed => "operation-failed",
            _ => throw new ArgumentOutOfRangeException(nameof(detail)),
        };
}
