using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<(int Downloaded, int Skipped)> RunManualPullDownloadsAsync(
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

        return (downloaded, skipped);
    }

    private static async Task<(int Downloaded, int Skipped)> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            if (!sync.CloudFileExists(path))
                return (Downloaded: 0, Skipped: 1);

            PatchHelper.Log(PullDownloading(path));
            string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
            await sync.WriteCloudContentAsync(path, content);
            PatchHelper.Log(PullWrote(path, content.Length));
            return (Downloaded: 1, Skipped: 0);
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return (Downloaded: 0, Skipped: 0);
    }
}
