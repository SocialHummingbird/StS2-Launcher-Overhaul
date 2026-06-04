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

    private static Task<string> RunManualPushUploadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var queued = sync.RunCloudBatch(() =>
        {
            var count = 0;
            foreach (var path in paths)
            {
                var result = QueueManualPushPath(sync, path);
                if (result.Queued)
                    count++;

                if (result.StopAfterBudget)
                    break;
            }

            return count;
        });

        return Task.FromResult(PushComplete(queued));
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
