using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<ManualCloudSyncResult> RunManualPullDownloadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var seedSession = await ModdedSaveSeedSession.PrepareAsync(sync, paths);
        sync.ReportTransferStarted(paths.Count);
        var downloadedCloudContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var summary = ManualSyncTransferSummary.Empty;
        foreach (var path in paths)
        {
            sync.CancellationToken.ThrowIfCancellationRequested();
            sync.ReportTransferPathStarted(path);
            var result = await PullManualPathAsync(sync, path, downloadedCloudContent);
            summary = summary.Include(result);
            sync.ReportTransferProcessed(
                path,
                result.Outcome
            );
            if (result.StopAfterBudget)
                break;
        }

        var seedSummary = await seedSession.CompleteAsync(sync, downloadedCloudContent);
        sync.ReportFinalizing(
            "Refreshing the launcher backup mirror and writing Pull evidence"
        );
        var mirror = sync.RefreshLocalBackupMirror();
        return summary.BuildResult(
            CloudOperationKind.Pull,
            paths.Count,
            sync.ProgressState,
            mirror.Errors,
            $"{seedSummary} Local backup mirror: {mirror}."
        );
    }

    private static async Task<ManualSyncPathResult> PullManualPathAsync(
        ManualSyncContext sync,
        string path,
        IDictionary<string, string> downloadedCloudContent
    )
    {
        var result = ManualSyncPathResult.FailedPath;
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
        catch (OperationCanceledException)
            when (sync.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            PatchHelper.Log(PullPathTimedOut(path));
            result = ManualSyncPathResult.TimedOutPath;
        }
        catch (Exception ex) when (IsCloudFileMissing(ex))
        {
            PatchHelper.Log(PullNotInCloud(path));
            result = ManualSyncPathResult.SkippedPath;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PullFailed(path, ex));
            result = ManualSyncPathResult.FailedPath;
        }

        return sync.BudgetExceeded(ManualPullBudgetExceeded())
            ? result.WithBudgetStop()
            : result;
    }
}
