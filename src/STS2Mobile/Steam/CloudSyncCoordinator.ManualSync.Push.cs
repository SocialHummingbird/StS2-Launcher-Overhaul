using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<ManualCloudSyncResult> RunManualPushUploadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        sync.ReportTransferStarted(paths.Count);
        var summary = ManualSyncTransferSummary.Empty;
        foreach (var path in paths)
        {
            sync.CancellationToken.ThrowIfCancellationRequested();
            sync.ReportTransferPathStarted(path);
            var result = await UploadManualPushPathAsync(
                sync,
                path
            );
            summary = summary.Include(result);
            sync.ReportTransferProcessed(
                path,
                result.Outcome
            );
            if (result.StopAfterBudget)
                break;
        }

        sync.ReportFinalizing("Writing manual Push evidence");
        return summary.BuildResult(
            CloudOperationKind.Push,
            paths.Count,
            sync.ProgressState,
            postProcessingErrorCount: 0,
            detail: ""
        );
    }

    private static async Task<ManualSyncPathResult> UploadManualPushPathAsync(
        ManualSyncContext sync,
        string path
    )
    {
        try
        {
            var local = await sync.ReadLocalFileAsync(path);
            if (local == null)
                return ManualSyncPathResult.SkippedPath;

            PatchHelper.Log(PushQueuing(path, local.Length));
            if (sync.BudgetExceeded(ManualPushBudgetExceeded()))
                return ManualSyncPathResult.BudgetExceeded;

            await sync.WriteCloudFileAsync(path, local);
            return ManualSyncPathResult.CompletedPath;
        }
        catch (OperationCanceledException)
            when (sync.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(PushFailed(path, ex));
            return ManualSyncPathResult.FailedPath;
        }
    }
}
