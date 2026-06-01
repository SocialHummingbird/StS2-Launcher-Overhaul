using System;
using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static class ManualPushBatch
    {
        private enum QueuePathResult
        {
            Skipped,
            Queued,
            Stop,
        }

        private static int Run(ManualSyncContext sync, IEnumerable<string> paths)
        {
            sync.BeginCloudBatch();
            int count = 0;
            try
            {
                foreach (var path in paths)
                {
                    switch (QueuePath(sync, path))
                    {
                        case QueuePathResult.Queued:
                            count++;
                            break;
                        case QueuePathResult.Stop:
                            return count;
                    }
                }
            }
            finally
            {
                sync.EndCloudBatch();
            }

            return count;
        }

        private static QueuePathResult QueuePath(ManualSyncContext sync, string path)
        {
            try
            {
                if (!sync.LocalFileExists(path))
                    return QueuePathResult.Skipped;

                string content = sync.ReadLocalFile(path);
                PatchHelper.Log(PushQueuing(path, content.Length));
                if (sync.BudgetExceeded(ManualPushBudgetExceeded))
                    return QueuePathResult.Stop;

                sync.WriteCloudFile(path, content);
                return QueuePathResult.Queued;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(PushFailed(path, ex));
                return QueuePathResult.Skipped;
            }
        }
    }
}
