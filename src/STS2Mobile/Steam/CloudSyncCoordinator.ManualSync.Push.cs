using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualPushResult
    {
        private ManualPushResult(int queued, bool stopAfterBudget)
        {
            Queued = queued;
            StopAfterBudget = stopAfterBudget;
        }

        private int Queued { get; }
        internal bool StopAfterBudget { get; }

        internal string CompleteMessage()
            => PushComplete(Queued);

        internal ManualPushResult Add(ManualPushResult result)
            => new(Queued + result.Queued, StopAfterBudget || result.StopAfterBudget);

        internal static ManualPushResult Empty()
            => new(queued: 0, stopAfterBudget: false);

        internal static ManualPushResult QueuedOne()
            => new(queued: 1, stopAfterBudget: false);

        internal static ManualPushResult Skipped()
            => new(queued: 0, stopAfterBudget: false);

        internal static ManualPushResult BudgetExceeded()
            => new(queued: 0, stopAfterBudget: true);
    }

    private static ManualPushResult RunManualPushUploads(
        ManualSyncContext sync,
        IEnumerable<string> paths
    )
        => sync.RunCloudBatch(() => QueueManualPushPaths(sync, paths));

    private static Task<ManualPushResult> RunManualPushUploadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
        => Task.FromResult(RunManualPushUploads(sync, paths));

    private static ManualPushResult QueueManualPushPaths(
        ManualSyncContext sync,
        IEnumerable<string> paths
    )
    {
        var total = ManualPushResult.Empty();
        foreach (var path in paths)
        {
            var result = QueueManualPushPath(sync, path);
            total = total.Add(result);

            if (result.StopAfterBudget)
                break;
        }

        return total;
    }

    private static ManualPushResult QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return ManualPushResult.Skipped();

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return ManualPushResult.BudgetExceeded();

            sync.WriteCloudFile(path, local);
            return ManualPushResult.QueuedOne();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualPushResult.Skipped();
        }
    }
}
