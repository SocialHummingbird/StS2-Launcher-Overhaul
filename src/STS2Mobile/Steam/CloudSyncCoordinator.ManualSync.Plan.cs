using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<ManualCloudSyncResult> RunManualSyncAsync(
        string accountName,
        string refreshToken,
        ManualSyncPlan plan,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => await RunManualSyncAsync(
            () => CreateManualSyncContext(
                accountName,
                refreshToken,
                progress,
                cancellationToken
            ),
            plan,
            progress,
            cancellationToken
        ).ConfigureAwait(false);

    private static async Task<ManualCloudSyncResult> RunManualSyncAsync(
        MegaCrit.Sts2.Core.Saves.ISaveStore local,
        MegaCrit.Sts2.Core.Saves.ICloudSaveStore cloud,
        ManualSyncPlan plan,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => await RunManualSyncAsync(
            () => CreateManualSyncContext(
                local,
                cloud,
                progress,
                cancellationToken
            ),
            plan,
            progress,
            cancellationToken
        ).ConfigureAwait(false);

    private static async Task<ManualCloudSyncResult> RunManualSyncAsync(
        Func<ManualSyncContext> createContext,
        ManualSyncPlan plan,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress.Preparing("Connecting to Steam Cloud");
        try
        {
            var sync = createContext();
            var result = await plan.RunAsync(sync);
            cancellationToken.ThrowIfCancellationRequested();
            switch (result.Completion)
            {
                case ManualCloudSyncCompletion.Success:
                    progress.Completed("All cloud sync steps completed");
                    break;
                case ManualCloudSyncCompletion.PartialSuccess:
                    progress.PartiallyCompleted(
                        "Cloud sync stopped after partial success"
                    );
                    break;
                default:
                    progress.Failed(
                        "Cloud sync did not transfer any save files"
                    );
                    break;
            }
            return result;
        }
        catch (Exception ex)
        {
            progress.Failed(ex.Message);
            throw;
        }
    }

    private readonly struct ManualSyncPlan
    {
        private ManualSyncPlan(
            ManualSyncPathDiscovery paths,
            ManualSyncBackupStep backup,
            ManualSyncTransferStep transfer
        )
        {
            Paths = paths;
            Backup = backup;
            Transfer = transfer;
        }

        internal static ManualSyncPlan Push { get; } = new(
            new ManualSyncPathDiscovery(
                sync => sync.DiscoverLocalPaths(),
                PushStarting
            ),
            new ManualSyncBackupStep(
                SaveBackups.BeforeManualPushAsync,
                PushBackedUpPrePushFiles,
                passCount: 2
            ),
            new ManualSyncTransferStep(RunManualPushUploadsAsync)
        );

        internal static ManualSyncPlan Pull { get; } = new(
            new ManualSyncPathDiscovery(
                sync => sync.DiscoverCloudPaths(),
                PullStarting
            ),
            new ManualSyncBackupStep(
                SaveBackups.LocalBeforeManualPullAsync,
                PullBackedUpLocalFiles
            ),
            new ManualSyncTransferStep(RunManualPullDownloadsAsync)
        );

        private ManualSyncPathDiscovery Paths { get; }
        private ManualSyncBackupStep Backup { get; }
        private ManualSyncTransferStep Transfer { get; }

        internal async Task<ManualCloudSyncResult> RunAsync(
            ManualSyncContext sync
        )
        {
            sync.CancellationToken.ThrowIfCancellationRequested();
            sync.ReportEnumerationStarted();
            var paths = Paths.Discover(sync);
            sync.CancellationToken.ThrowIfCancellationRequested();
            sync.ReportEnumerationCompleted(paths.Count);
            await Backup.RunAsync(sync, paths).ConfigureAwait(false);
            sync.CancellationToken.ThrowIfCancellationRequested();
            return await Transfer.RunAsync(sync, paths)
                .ConfigureAwait(false);
        }
    }

    private readonly struct ManualSyncPathDiscovery
    {
        internal ManualSyncPathDiscovery(
            Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
            Func<int, string> startingMessage
        )
        {
            DiscoverPaths = discoverPaths;
            StartingMessage = startingMessage;
        }

        private Func<ManualSyncContext, IReadOnlyCollection<string>> DiscoverPaths { get; }
        private Func<int, string> StartingMessage { get; }

        internal IReadOnlyCollection<string> Discover(ManualSyncContext sync)
        {
            var paths = DiscoverPaths(sync);
            PatchHelper.Log(StartingMessage(paths.Count));
            PatchHelper.Log(
                "[Cloud] Candidate sync paths: "
                    + string.Join(", ", paths.Take(25))
                    + (paths.Count > 25 ? $", ... +{paths.Count - 25} more" : "")
            );
            return paths;
        }
    }

    private readonly struct ManualSyncBackupStep
    {
        internal ManualSyncBackupStep(
            Func<ManualSyncContext, IEnumerable<string>, Task<int>> backupAsync,
            Func<int, string> backedUpMessage,
            int passCount = 1
        )
        {
            BackupAsync = backupAsync;
            BackedUpMessage = backedUpMessage;
            PassCount = passCount;
        }

        private Func<ManualSyncContext, IEnumerable<string>, Task<int>> BackupAsync { get; }
        private Func<int, string> BackedUpMessage { get; }
        private int PassCount { get; }

        internal async Task RunAsync(
            ManualSyncContext sync,
            IReadOnlyCollection<string> paths
        )
        {
            sync.ReportBackupStarted(paths.Count * PassCount);
            var backedUp = await BackupAsync(sync, paths)
                .ConfigureAwait(false);
            sync.CancellationToken.ThrowIfCancellationRequested();
            if (backedUp > 0)
                PatchHelper.Log(BackedUpMessage(backedUp));
        }
    }

    private readonly struct ManualSyncTransferStep
    {
        internal ManualSyncTransferStep(
            Func<
                ManualSyncContext,
                IReadOnlyCollection<string>,
                Task<ManualCloudSyncResult>
            > transferAsync
        )
        {
            TransferAsync = transferAsync;
        }

        private Func<
            ManualSyncContext,
            IReadOnlyCollection<string>,
            Task<ManualCloudSyncResult>
        > TransferAsync { get; }

        internal async Task<ManualCloudSyncResult> RunAsync(
            ManualSyncContext sync,
            IReadOnlyCollection<string> paths
        )
        {
            var result = await TransferAsync(sync, paths)
                .ConfigureAwait(false);
            sync.CancellationToken.ThrowIfCancellationRequested();
            PatchHelper.Log(
                $"[Cloud] Manual sync result: completion={result.Completion}; "
                    + $"candidates={result.CandidatePathCount}; "
                    + $"completed={result.CompletedPathCount}; "
                    + $"skipped={result.SkippedPathCount}; "
                    + $"failed={result.FailedPathCount}; "
                    + $"timedOut={result.TimedOutPathCount}; "
                    + $"unfinished={result.UnprocessedPathCount}; "
                    + $"postErrors={result.PostProcessingErrorCount}"
            );
            if (!string.IsNullOrWhiteSpace(result.Detail))
                PatchHelper.Log(result.Detail);
            return result;
        }
    }
}
