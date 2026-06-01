using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualPullResult
    {
        private ManualPullResult(int downloaded, int skipped)
        {
            Downloaded = downloaded;
            Skipped = skipped;
        }

        private int Downloaded { get; }
        private int Skipped { get; }

        internal string CompleteMessage()
            => PullComplete(Downloaded, Skipped);

        internal ManualPullResult Add(ManualPullResult result)
            => new(Downloaded + result.Downloaded, Skipped + result.Skipped);

        internal static ManualPullResult DownloadedOne()
            => new(downloaded: 1, skipped: 0);

        internal static ManualPullResult Empty()
            => new(downloaded: 0, skipped: 0);

        internal static ManualPullResult SkippedOne()
            => new(downloaded: 0, skipped: 1);

        internal static ManualPullResult Failed()
            => new(downloaded: 0, skipped: 0);
    }

    private static async Task<ManualPullResult> RunManualPullDownloadsAsync(
        ManualSyncContext sync,
        IEnumerable<string> paths
    )
    {
        var total = ManualPullResult.Empty();
        foreach (var path in paths)
        {
            var result = await PullManualPathAsync(sync, path);
            total = total.Add(result);

            if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                break;
        }

        return total;
    }

    private static async Task<ManualPullResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            if (!sync.CloudFileExists(path))
                return ManualPullResult.SkippedOne();

            PatchHelper.Log(PullDownloading(path));
            string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
            await sync.WriteLocalContentFromCloudAsync(path, content);
            PatchHelper.Log(PullWrote(path, content.Length));
            return ManualPullResult.DownloadedOne();
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return ManualPullResult.Failed();
    }
}
