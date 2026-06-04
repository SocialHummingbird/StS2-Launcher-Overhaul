using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private enum ManualPushPathState
    {
        Skipped,
        Queued,
        BudgetExceeded,
    }

    private readonly struct ManualPushPathResult
    {
        private ManualPushPathResult(ManualPushPathState state)
        {
            State = state;
        }

        private ManualPushPathState State { get; }
        internal bool Queued => State == ManualPushPathState.Queued;
        internal bool StopAfterBudget => State == ManualPushPathState.BudgetExceeded;

        internal static ManualPushPathResult Skipped()
            => new(ManualPushPathState.Skipped);

        internal static ManualPushPathResult QueuedPath()
            => new(ManualPushPathState.Queued);

        internal static ManualPushPathResult BudgetExceeded()
            => new(ManualPushPathState.BudgetExceeded);
    }

    private readonly struct ManualPushResultTotals
    {
        private ManualPushResultTotals(int queued)
        {
            Queued = queued;
        }

        private int Queued { get; }

        internal static ManualPushResultTotals Empty()
            => new(0);

        internal ManualPushResultTotals Add(ManualPushPathResult result)
            => result.Queued ? new(Queued + 1) : this;

        internal string CompleteMessage()
            => PushComplete(Queued);
    }

    private static Task<string> RunManualPushUploadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var totals = sync.RunCloudBatch(() =>
        {
            var batchTotals = ManualPushResultTotals.Empty();
            foreach (var path in paths)
            {
                var result = QueueManualPushPath(sync, path);
                batchTotals = batchTotals.Add(result);

                if (result.StopAfterBudget)
                    break;
            }

            return batchTotals;
        });

        return Task.FromResult(totals.CompleteMessage());
    }

    private static ManualPushPathResult QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return ManualPushPathResult.Skipped();

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return ManualPushPathResult.BudgetExceeded();

            sync.WriteCloudFile(path, local);
            return ManualPushPathResult.QueuedPath();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualPushPathResult.Skipped();
        }
    }
}
