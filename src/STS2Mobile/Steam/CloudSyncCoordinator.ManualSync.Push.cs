using System;
using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static int RunManualPushUploads(ManualSyncContext sync, IEnumerable<string> paths)
        => sync.RunCloudBatch(() => QueueManualPushPaths(sync, paths));

    private static int QueueManualPushPaths(ManualSyncContext sync, IEnumerable<string> paths)
    {
        int count = 0;
        foreach (var path in paths)
        {
            var result = QueueManualPushPath(sync, path);
            if (result.Stop)
                return count;

            if (result.Queued)
                count++;
        }

        return count;
    }

    private static (bool Queued, bool Stop) QueueManualPushPath(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = sync.ReadLocalFile(path);
            if (local == null)
                return (Queued: false, Stop: false);

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                return (Queued: false, Stop: true);

            sync.WriteCloudFile(path, local);
            return (Queued: true, Stop: false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return (Queued: false, Stop: false);
        }
    }
}
