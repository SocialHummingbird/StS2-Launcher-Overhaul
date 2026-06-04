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
        var queuedCount = sync.RunCloudBatch(() =>
        {
            var batchQueued = 0;
            foreach (var path in paths)
            {
                var (pathQueued, stopAfterBudget) = QueueManualPushPath(sync, path);
                batchQueued += pathQueued;
                if (stopAfterBudget)
                    break;
            }

            return batchQueued;
        });

        return Task.FromResult(PushComplete(queuedCount));
    }

    private static (int pathQueued, bool stopAfterBudget) QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return (0, false);

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return (0, true);

            sync.WriteCloudFile(path, local);
            return (1, false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return (0, false);
        }
    }
}
