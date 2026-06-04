using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private delegate IReadOnlyCollection<string> ManualSyncPathDiscovery(
        ManualSyncContext sync
    );

    private delegate Task<int> ManualSyncBackup(
        ManualSyncContext sync,
        IEnumerable<string> paths
    );

    private delegate Task<ManualSyncResultAccumulator> ManualSyncTransfer(
        ManualSyncContext sync,
        IReadOnlyCollection<string> paths
    );

    private static readonly ManualSyncPlan ManualPushPlan =
        ManualSyncPlan.Push(RunManualPushUploadsAsync);

    private static readonly ManualSyncPlan ManualPullPlan =
        ManualSyncPlan.Pull(RunManualPullDownloadsAsync);

    private readonly struct ManualSyncPlan
    {
        private readonly ManualSyncPathDiscovery _discoverPaths;
        private readonly Func<int, string> _startingMessage;
        private readonly ManualSyncBackup _backupAsync;
        private readonly Func<int, string> _backedUpMessage;
        private readonly ManualSyncTransfer _transferAsync;
        private readonly Func<int, int, int, string> _completeMessage;

        private ManualSyncPlan(
            ManualSyncPathDiscovery discoverPaths,
            Func<int, string> startingMessage,
            ManualSyncBackup backupAsync,
            Func<int, string> backedUpMessage,
            ManualSyncTransfer transferAsync,
            Func<int, int, int, string> completeMessage
        )
        {
            _discoverPaths = discoverPaths;
            _startingMessage = startingMessage;
            _backupAsync = backupAsync;
            _backedUpMessage = backedUpMessage;
            _transferAsync = transferAsync;
            _completeMessage = completeMessage;
        }

        internal static ManualSyncPlan Push(ManualSyncTransfer transferAsync)
            => new(
                sync => sync.DiscoverLocalPaths(),
                PushStarting,
                SaveBackups.CloudBeforeManualPushAsync,
                PushBackedUpCloudFiles,
                transferAsync,
                (queued, _, _) => PushComplete(queued)
            );

        internal static ManualSyncPlan Pull(ManualSyncTransfer transferAsync)
            => new(
                sync => sync.DiscoverCloudPaths(),
                PullStarting,
                SaveBackups.LocalBeforeManualPullAsync,
                PullBackedUpLocalFiles,
                transferAsync,
                (_, downloaded, skipped) => PullComplete(downloaded, skipped)
            );

        internal async Task RunAsync(string accountName, string refreshToken)
        {
            var sync = CreateManualSyncContext(accountName, refreshToken);
            var paths = _discoverPaths(sync);
            PatchHelper.Log(_startingMessage(paths.Count));

            var backedUp = await _backupAsync(sync, paths);
            if (backedUp > 0)
                PatchHelper.Log(_backedUpMessage(backedUp));

            var transferResult = await _transferAsync(sync, paths);
            PatchHelper.Log(transferResult.CompleteMessage(_completeMessage));
        }
    }
}
