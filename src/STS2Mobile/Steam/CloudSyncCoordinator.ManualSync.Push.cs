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
        var totals = sync.RunCloudBatch(() =>
        {
            var batchTotals = ManualSyncResultTotals.Empty();
            foreach (var path in paths)
            {
                var result = QueueManualPushPath(sync, path);
                batchTotals = batchTotals.Add(result);

                if (result.StopAfterBudget)
                    break;
            }

            return batchTotals;
        });

        return Task.FromResult(totals.PushCompleteMessage());
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
                return ManualSyncPathResult.NoChange();

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return ManualSyncPathResult.BudgetExceeded();

            sync.WriteCloudFile(path, local);
            return ManualSyncPathResult.QueuedPath();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualSyncPathResult.NoChange();
        }
    }
}
