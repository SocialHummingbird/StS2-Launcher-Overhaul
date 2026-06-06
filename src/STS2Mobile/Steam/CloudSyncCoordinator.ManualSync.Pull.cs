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
            var result = await PullManualPathAsync(sync, path);
            summary = summary.Include(result);
            if (result.StopAfterBudget)
                break;
        }

        return summary.CompleteMessage();
    }

    private static async Task<ManualSyncPathResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        var result = ManualSyncPathResult.Ignored;
        try
        {
            if (!sync.CloudFileExists(path))
                result = ManualSyncPathResult.SkippedPath;
            else
            {
                PatchHelper.Log(PullDownloading(path));
                string content = await sync.ReadCloudContentAsync(path, ManualPullDownloadOperation);
                await sync.WriteLocalContentFromCloudAsync(path, content);
                PatchHelper.Log(PullWrote(path, content.Length));
                result = ManualSyncPathResult.CompletedPath;
            }
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
        }

        return sync.BudgetExceeded(ManualPullBudgetExceeded())
            ? result.WithBudgetStop()
            : result;
    }
}
