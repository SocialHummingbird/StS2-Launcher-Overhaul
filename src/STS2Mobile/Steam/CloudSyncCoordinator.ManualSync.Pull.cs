using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualPullPathResult
    {
        private ManualPullPathResult(int downloaded, int skipped)
        {
            Downloaded = downloaded;
            Skipped = skipped;
        }

        internal int Downloaded { get; }
        internal int Skipped { get; }

        internal static ManualPullPathResult DownloadedPath()
            => new(1, 0);

        internal static ManualPullPathResult SkippedMissingCloud()
            => new(0, 1);

        internal static ManualPullPathResult Failed()
            => new(0, 0);
    }

    private static async Task<string> RunManualPullDownloadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var downloaded = 0;
        var skipped = 0;
        foreach (var path in paths)
        {
            var result = await PullManualPathAsync(sync, path);
            downloaded += result.Downloaded;
            skipped += result.Skipped;

            if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                break;
        }

        return PullComplete(downloaded, skipped);
    }

    private static async Task<ManualPullPathResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            if (!sync.CloudFileExists(path))
                return ManualPullPathResult.SkippedMissingCloud();

            PatchHelper.Log(PullDownloading(path));
            string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
            await sync.WriteLocalContentFromCloudAsync(path, content);
            PatchHelper.Log(PullWrote(path, content.Length));
            return ManualPullPathResult.DownloadedPath();
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return ManualPullPathResult.Failed();
    }
}
