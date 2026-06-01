using System;
using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private enum ManualPushQueueResult
    {
        Skipped,
        Queued,
        StopAfterBudget,
    }

    private static int RunManualPushUploads(ManualSyncContext sync, IEnumerable<string> paths)
        => sync.RunCloudBatch(() => QueueManualPushPaths(sync, paths));

    private static int QueueManualPushPaths(ManualSyncContext sync, IEnumerable<string> paths)
    {
        int count = 0;
        foreach (var path in paths)
        {
            var result = QueueManualPushPath(sync, path);
            if (result == ManualPushQueueResult.StopAfterBudget)
                return count;

            if (result == ManualPushQueueResult.Queued)
                count++;
        }

        return count;
    }

    private static ManualPushQueueResult QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return ManualPushQueueResult.Skipped;

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return ManualPushQueueResult.StopAfterBudget;

            sync.WriteCloudFile(path, local);
            return ManualPushQueueResult.Queued;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualPushQueueResult.Skipped;
        }
    }
}
