using System;

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

    private sealed class ManualSyncResultAccumulator
    {
        private int _downloaded;
        private int _queued;
        private int _skipped;

        internal static ManualSyncResultAccumulator Empty()
            => new();

        internal bool Add(ManualSyncPathResult result)
        {
            _queued += result.Queued;
            _downloaded += result.Downloaded;
            _skipped += result.Skipped;
            return result.StopAfterBudget;
        }

        internal string CompleteMessage(Func<int, int, int, string> format)
            => format(_queued, _downloaded, _skipped);
    }
}
