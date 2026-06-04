using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private enum ManualPullPathState
    {
        Failed,
        Downloaded,
        SkippedMissingCloud,
    }

    private readonly struct ManualPullPathResult
    {
        private ManualPullPathResult(ManualPullPathState state)
        {
            State = state;
        }

        private ManualPullPathState State { get; }
        internal int Downloaded => State == ManualPullPathState.Downloaded ? 1 : 0;
        internal int Skipped => State == ManualPullPathState.SkippedMissingCloud ? 1 : 0;

        internal static ManualPullPathResult DownloadedPath()
            => new(ManualPullPathState.Downloaded);

        internal static ManualPullPathResult SkippedMissingCloud()
            => new(ManualPullPathState.SkippedMissingCloud);

        internal static ManualPullPathResult Failed()
            => new(ManualPullPathState.Failed);
    }

    private readonly struct ManualPullResultTotals
    {
        private ManualPullResultTotals(int downloaded, int skipped)
        {
            Downloaded = downloaded;
            Skipped = skipped;
        }

        private int Downloaded { get; }
        private int Skipped { get; }

        internal static ManualPullResultTotals Empty()
            => new(0, 0);

        internal ManualPullResultTotals Add(ManualPullPathResult result)
            => new(Downloaded + result.Downloaded, Skipped + result.Skipped);

        internal string CompleteMessage()
            => PullComplete(Downloaded, Skipped);
    }

    private static async Task<string> RunManualPullDownloadsAsync(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    )
    {
        var totals = ManualPullResultTotals.Empty();
        foreach (var path in paths)
        {
            var result = await PullManualPathAsync(sync, path);
            totals = totals.Add(result);

            if (sync.BudgetExceeded(ManualPullBudgetExceeded))
                break;
        }

        return totals.CompleteMessage();
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
