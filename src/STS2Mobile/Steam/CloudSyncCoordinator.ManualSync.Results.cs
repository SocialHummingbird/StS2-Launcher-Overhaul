namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualSyncPathResult
    {
        private ManualSyncPathResult(
            int completed,
            int skipped,
            int failed,
            int timedOut,
            bool stopAfterBudget
        )
        {
            Completed = completed;
            Skipped = skipped;
            Failed = failed;
            TimedOut = timedOut;
            StopAfterBudget = stopAfterBudget;
        }

        internal int Completed { get; }
        internal int Skipped { get; }
        internal int Failed { get; }
        internal int TimedOut { get; }
        internal bool StopAfterBudget { get; }
        internal int Processed
            => Completed + Skipped + Failed + TimedOut;

        internal static ManualSyncPathResult CompletedPath { get; } =
            new(1, 0, 0, 0, false);
        internal static ManualSyncPathResult SkippedPath { get; } =
            new(0, 1, 0, 0, false);
        internal static ManualSyncPathResult FailedPath { get; } =
            new(0, 0, 1, 0, false);
        internal static ManualSyncPathResult TimedOutPath { get; } =
            new(0, 0, 0, 1, false);
        internal static ManualSyncPathResult BudgetExceeded { get; } =
            new(0, 0, 0, 1, true);

        internal ManualSyncPathResult WithBudgetStop()
            => new(Completed, Skipped, Failed, TimedOut, true);

        internal CloudTransferPathOutcome Outcome
            => Completed > 0
                ? CloudTransferPathOutcome.Completed
                : Skipped > 0
                    ? CloudTransferPathOutcome.Skipped
                    : TimedOut > 0
                        ? CloudTransferPathOutcome.TimedOut
                        : CloudTransferPathOutcome.Failed;
    }

    private readonly struct ManualSyncTransferSummary
    {
        private ManualSyncTransferSummary(
            int completed,
            int skipped,
            int failed,
            int timedOut
        )
        {
            Completed = completed;
            Skipped = skipped;
            Failed = failed;
            TimedOut = timedOut;
        }

        private int Completed { get; }
        private int Skipped { get; }
        private int Failed { get; }
        private int TimedOut { get; }

        internal static ManualSyncTransferSummary Empty { get; } =
            new(0, 0, 0, 0);

        internal ManualSyncTransferSummary Include(ManualSyncPathResult result)
            => new(
                Completed + result.Completed,
                Skipped + result.Skipped,
                Failed + result.Failed,
                TimedOut + result.TimedOut
            );

        internal ManualCloudSyncResult BuildResult(
            CloudOperationKind kind,
            int candidatePathCount,
            CloudOperationState progress,
            int postProcessingErrorCount,
            string detail
        )
        {
            var processed = Completed + Skipped + Failed + TimedOut;
            return new ManualCloudSyncResult(
                kind,
                candidatePathCount,
                Completed,
                Skipped,
                Failed,
                TimedOut,
                System.Math.Max(0, candidatePathCount - processed),
                progress.BackupCreatedCount,
                progress.PrivateBackupCreatedCount,
                progress.ProfileSeededCount,
                postProcessingErrorCount,
                detail
            );
        }
    }
}
