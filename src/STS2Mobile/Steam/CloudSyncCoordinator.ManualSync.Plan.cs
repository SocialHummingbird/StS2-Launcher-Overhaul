using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<string> RunManualSyncAsync(
        string accountName,
        string refreshToken,
        ManualSyncPlan plan
    )
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        return await plan.RunAsync(sync);
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
                PushBackedUpPrePushFiles
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

        internal async Task<string> RunAsync(ManualSyncContext sync)
        {
            var paths = Paths.Discover(sync);
            await Backup.RunAsync(sync, paths);
            return await Transfer.RunAsync(sync, paths);
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
            Func<int, string> backedUpMessage
        )
        {
            BackupAsync = backupAsync;
            BackedUpMessage = backedUpMessage;
        }

        private Func<ManualSyncContext, IEnumerable<string>, Task<int>> BackupAsync { get; }
        private Func<int, string> BackedUpMessage { get; }

        internal async Task RunAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
        {
            var backedUp = await BackupAsync(sync, paths);
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
                Task<string>
            > transferAsync
        )
        {
            TransferAsync = transferAsync;
        }

        private Func<
            ManualSyncContext,
            IReadOnlyCollection<string>,
            Task<string>
        > TransferAsync { get; }

        internal async Task<string> RunAsync(
            ManualSyncContext sync,
            IReadOnlyCollection<string> paths
        )
        {
            var result = await TransferAsync(sync, paths);
            PatchHelper.Log(result);
            return result;
        }
    }
}
