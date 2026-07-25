#nullable enable

namespace STS2Mobile.Steam;

internal enum ManualCloudSyncCompletion
{
    Success,
    PartialSuccess,
    Failure,
}

internal readonly record struct ManualCloudSyncResult(
    CloudOperationKind Kind,
    int CandidatePathCount,
    int CompletedPathCount,
    int SkippedPathCount,
    int FailedPathCount,
    int TimedOutPathCount,
    int UnprocessedPathCount,
    int BackupCreatedCount,
    int PrivateBackupCreatedCount,
    int ProfileSeededCount,
    int PostProcessingErrorCount,
    string Detail
)
{
    internal int FaultCount
        => FailedPathCount
            + TimedOutPathCount
            + UnprocessedPathCount
            + PostProcessingErrorCount;

    internal ManualCloudSyncCompletion Completion
    {
        get
        {
            if (CompletedPathCount <= 0)
                return ManualCloudSyncCompletion.Failure;

            return FaultCount > 0
                ? ManualCloudSyncCompletion.PartialSuccess
                : ManualCloudSyncCompletion.Success;
        }
    }

    internal bool CanRecordCompletionEvidence
        => Completion == ManualCloudSyncCompletion.Success;
}
