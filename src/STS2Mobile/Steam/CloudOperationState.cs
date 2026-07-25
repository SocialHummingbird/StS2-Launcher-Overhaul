namespace STS2Mobile.Steam;

internal enum CloudOperationKind
{
    Pull,
    Push,
}

internal enum CloudOperationPhase
{
    Idle,
    Preparing,
    Enumerating,
    BackingUp,
    PreparingProfiles,
    Transferring,
    SeedingProfiles,
    Finalizing,
    Completed,
    PartiallyCompleted,
    Failed,
}

internal enum CloudTransferPathOutcome
{
    Completed,
    Skipped,
    Failed,
    TimedOut,
}

internal readonly record struct CloudOperationState(
    CloudOperationKind Kind,
    CloudOperationPhase Phase,
    string CurrentItem,
    int EnumeratedPathCount,
    int BackupProcessedCount,
    int BackupTotalCount,
    int BackupCreatedCount,
    int ProfilePreparationProcessedCount,
    int ProfilePreparationTotalCount,
    int PrivateBackupCreatedCount,
    int TransferProcessedCount,
    int TransferTotalCount,
    int TransferCompletedCount,
    int TransferSkippedCount,
    int TransferFailedCount,
    int TransferTimedOutCount,
    int ProfileSeedProcessedCount,
    int ProfileSeedTotalCount,
    int ProfileSeededCount,
    string ErrorMessage
)
{
    internal bool IsActive
        => Phase is not CloudOperationPhase.Idle
            and not CloudOperationPhase.Completed
            and not CloudOperationPhase.PartiallyCompleted
            and not CloudOperationPhase.Failed;

    internal bool IsTerminal
        => Phase is CloudOperationPhase.Completed
            or CloudOperationPhase.PartiallyCompleted
            or CloudOperationPhase.Failed;

    internal static CloudOperationState Idle(CloudOperationKind kind)
        => new(
            kind,
            CloudOperationPhase.Idle,
            "",
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            ""
        );
}
