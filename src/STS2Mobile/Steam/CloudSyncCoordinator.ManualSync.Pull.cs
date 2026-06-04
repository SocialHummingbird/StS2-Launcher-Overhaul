using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
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
            downloaded += result.downloaded;
            skipped += result.skipped;

            if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                break;
        }

        return PullComplete(downloaded, skipped);
    }

    private static async Task<(int downloaded, int skipped)> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            if (!sync.CloudFileExists(path))
                return (0, 1);

            PatchHelper.Log(PullDownloading(path));
            string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
            await sync.WriteLocalContentFromCloudAsync(path, content);
            PatchHelper.Log(PullWrote(path, content.Length));
            return (1, 0);
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return (0, 0);
    }
}
