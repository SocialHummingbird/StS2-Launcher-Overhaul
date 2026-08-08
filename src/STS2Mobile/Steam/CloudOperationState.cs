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
    Transferring,
    Finalizing,
    Completed,
    Failed,
}

internal readonly record struct CloudOperationState(
    CloudOperationKind Kind,
    CloudOperationPhase Phase,
    string CurrentItem,
    int EnumeratedPathCount,
    int BackupProcessedCount,
    int BackupTotalCount,
    int BackupCreatedCount,
    int TransferProcessedCount,
    int TransferTotalCount,
    int TransferCompletedCount,
    string ErrorMessage
)
{
    internal bool IsActive
        => Phase is not CloudOperationPhase.Idle
            and not CloudOperationPhase.Completed
            and not CloudOperationPhase.Failed;

    internal bool IsTerminal
        => Phase is CloudOperationPhase.Completed
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
            ""
        );
}
