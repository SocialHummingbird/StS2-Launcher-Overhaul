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
        var summary = ManualSyncTransferSummary.Empty(PullComplete);
        foreach (var path in paths)
        {
            summary = summary.Include(await PullManualPathAsync(sync, path));

            if (sync.BudgetExceeded(ManualPullBudgetExceeded()))
                break;
        }

        return summary.CompleteMessage();
    }

    private static async Task<ManualSyncPathResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            if (!sync.CloudFileExists(path))
                return ManualSyncPathResult.SkippedPath;

            PatchHelper.Log(PullDownloading(path));
            string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
            await sync.WriteLocalContentFromCloudAsync(path, content);
            PatchHelper.Log(PullWrote(path, content.Length));
            return ManualSyncPathResult.CompletedPath;
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return ManualSyncPathResult.Ignored;
    }
}
