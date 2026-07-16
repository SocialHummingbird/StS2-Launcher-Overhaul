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
        var seedSession = await ModdedSaveSeedSession.PrepareAsync(sync, paths);
        var downloadedCloudContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var summary = ManualSyncTransferSummary.Empty(PullComplete);
        foreach (var path in paths)
        {
            var result = await PullManualPathAsync(sync, path, downloadedCloudContent);
            summary = summary.Include(result);
            if (result.StopAfterBudget)
                break;
        }

        var seedSummary = await seedSession.CompleteAsync(sync, downloadedCloudContent);
        return $"{summary.CompleteMessage()} {seedSummary}";
    }

    private static async Task<ManualSyncPathResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path,
        IDictionary<string, string> downloadedCloudContent
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
                downloadedCloudContent[path] = content;
                PatchHelper.Log(PullWrote(path, content.Length));
                result = ManualSyncPathResult.CompletedPath;
            }
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
        }
        catch (Exception ex) when (IsCloudFileMissing(ex))
        {
            PatchHelper.Log(PullNotInCloud(path));
            result = ManualSyncPathResult.SkippedPath;
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
