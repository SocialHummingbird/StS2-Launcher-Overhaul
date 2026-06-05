using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static Task<string> RunManualPushUploadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var summary = sync.RunCloudBatch(() =>
        {
            var batch = ManualSyncTransferSummary.Empty(
                (queued, _) => PushComplete(queued)
            );
            foreach (var path in paths)
            {
                var result = QueueManualPushPath(sync, path);
                batch = batch.Include(result);
                if (result.StopAfterBudget)
                    break;
            }

            return batch;
        });

        return Task.FromResult(summary.CompleteMessage());
    }

    private static ManualSyncPathResult QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return ManualSyncPathResult.Ignored;

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded()))
                return ManualSyncPathResult.BudgetExceeded;

            sync.WriteCloudFile(path, local);
            return ManualSyncPathResult.CompletedPath;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualSyncPathResult.Ignored;
        }
    }
}
