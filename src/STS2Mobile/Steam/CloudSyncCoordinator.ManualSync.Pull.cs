using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static class ManualPullBatch
    {
        internal static async Task<ManualPullCounts> RunAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
        {
            int downloaded = 0;
            int skipped = 0;
            foreach (var path in paths)
            {
                var result = await PullPathAsync(sync, path);
                downloaded += result.Downloaded;
                skipped += result.Skipped;

                if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                    break;
            }

            return new ManualPullCounts(downloaded, skipped);
        }

        private static async Task<ManualPullCounts> PullPathAsync(
            ManualSyncContext sync,
            string path
        )
        {
            try
            {
                if (!sync.CloudFileExists(path))
                    return new ManualPullCounts(0, 1);

                PatchHelper.Log(PullDownloading(path));
                string content = await sync.ReadCloudContentAsync(path, "ManualPull download");
                await sync.WriteCloudContentAsync(path, content);
                PatchHelper.Log(PullWrote(path, content.Length));
                return new ManualPullCounts(1, 0);
            }
            catch (TimeoutException)
            {
                PatchHelper.Log(PullPathTimedOut(path));
            }
            catch (Exception ex)
            {
                PatchHelper.Log(PullFailed(path, ex));
            }

            return new ManualPullCounts(0, 0);
        }
    }
}
