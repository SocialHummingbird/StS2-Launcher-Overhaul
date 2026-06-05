using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualSyncPathResult
    {
        private ManualSyncPathResult(int completed, int skipped, bool stopAfterBudget)
        {
            Completed = completed;
            Skipped = skipped;
            StopAfterBudget = stopAfterBudget;
        }

        internal int Completed { get; }
        internal int Skipped { get; }
        internal bool StopAfterBudget { get; }

        internal static ManualSyncPathResult Ignored { get; } = new(0, 0, false);
        internal static ManualSyncPathResult CompletedPath { get; } = new(1, 0, false);
        internal static ManualSyncPathResult SkippedPath { get; } = new(0, 1, false);
        internal static ManualSyncPathResult BudgetExceeded { get; } = new(0, 0, true);
    }

    private readonly struct ManualSyncTransferSummary
    {
        private ManualSyncTransferSummary(
            int completed,
            int skipped,
            Func<int, int, string> completeMessage
        )
        {
            Completed = completed;
            Skipped = skipped;
            CompleteMessageBuilder = completeMessage;
        }

        private int Completed { get; }
        private int Skipped { get; }
        private Func<int, int, string> CompleteMessageBuilder { get; }

        internal static ManualSyncTransferSummary Empty(
            Func<int, int, string> completeMessage
        )
            => new(0, 0, completeMessage);

        internal ManualSyncTransferSummary Include(ManualSyncPathResult result)
            => new(
                Completed + result.Completed,
                Skipped + result.Skipped,
                CompleteMessageBuilder
            );

        internal string CompleteMessage()
            => CompleteMessageBuilder(Completed, Skipped);
    }
}
