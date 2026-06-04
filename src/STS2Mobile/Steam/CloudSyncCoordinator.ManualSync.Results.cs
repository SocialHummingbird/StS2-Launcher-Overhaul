namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualSyncPathResult
    {
        private ManualSyncPathResult(
            int queued,
            int downloaded,
            int skipped,
            bool stopAfterBudget
        )
        {
            Queued = queued;
            Downloaded = downloaded;
            Skipped = skipped;
            StopAfterBudget = stopAfterBudget;
        }

        internal int Queued { get; }
        internal int Downloaded { get; }
        internal int Skipped { get; }
        internal bool StopAfterBudget { get; }

        internal static ManualSyncPathResult NoChange()
            => new(0, 0, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult QueuedPath()
            => new(1, 0, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult DownloadedPath()
            => new(0, 1, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult SkippedMissingCloud()
            => new(0, 0, 1, stopAfterBudget: false);

        internal static ManualSyncPathResult BudgetExceeded()
            => new(0, 0, 0, stopAfterBudget: true);
    }

    private readonly struct ManualSyncResultTotals
    {
        private ManualSyncResultTotals(int queued, int downloaded, int skipped)
        {
            Queued = queued;
            Downloaded = downloaded;
            Skipped = skipped;
        }

        private int Queued { get; }
        private int Downloaded { get; }
        private int Skipped { get; }

        internal static ManualSyncResultTotals Empty()
            => new(0, 0, 0);

        internal ManualSyncResultTotals Add(ManualSyncPathResult result)
            => new(
                Queued + result.Queued,
                Downloaded + result.Downloaded,
                Skipped + result.Skipped
            );

        internal string PushCompleteMessage()
            => PushComplete(Queued);

        internal string PullCompleteMessage()
            => PullComplete(Downloaded, Skipped);
    }

    private sealed class ManualSyncResultAccumulator
    {
        private ManualSyncResultTotals _totals = ManualSyncResultTotals.Empty();

        internal static ManualSyncResultAccumulator Empty()
            => new();

        internal bool Add(ManualSyncPathResult result)
        {
            _totals = _totals.Add(result);
            return result.StopAfterBudget;
        }

        internal string PushCompleteMessage()
            => _totals.PushCompleteMessage();

        internal string PullCompleteMessage()
            => _totals.PullCompleteMessage();
    }
}
