using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualPullResult
    {
        internal ManualPullResult(int downloaded, int skipped)
        {
            Downloaded = downloaded;
            Skipped = skipped;
        }

        internal int Downloaded { get; }
        internal int Skipped { get; }

        internal static ManualPullResult DownloadedOne()
            => new(downloaded: 1, skipped: 0);

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
        int downloaded = 0;
        int skipped = 0;
        foreach (var path in paths)
        {
            var result = await PullManualPathAsync(sync, path);
            downloaded += result.Downloaded;
            skipped += result.Skipped;

            if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                break;
        }

        return new ManualPullResult(downloaded, skipped);
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
