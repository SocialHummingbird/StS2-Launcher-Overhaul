using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static readonly ManualSyncPlan ManualPushPlan =
        ManualSyncPlan.Push(RunManualPushUploadsAsync);

    private static readonly ManualSyncPlan ManualPullPlan =
        ManualSyncPlan.Pull(RunManualPullDownloadsAsync);

    private readonly struct ManualSyncPlan
    {
        private readonly Func<ManualSyncContext, IReadOnlyCollection<string>> _discoverPaths;
        private readonly Func<int, string> _startingMessage;
        private readonly Func<ManualSyncContext, IEnumerable<string>, Task<int>> _backupAsync;
        private readonly Func<int, string> _backedUpMessage;
        private readonly Func<ManualSyncContext, IReadOnlyCollection<string>, Task<string>>
            _transferAsync;

        private ManualSyncPlan(
            Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
            Func<int, string> startingMessage,
            Func<ManualSyncContext, IEnumerable<string>, Task<int>> backupAsync,
            Func<int, string> backedUpMessage,
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<string>> transferAsync
        )
        {
            _discoverPaths = discoverPaths;
            _startingMessage = startingMessage;
            _backupAsync = backupAsync;
            _backedUpMessage = backedUpMessage;
            _transferAsync = transferAsync;
        }

        internal static ManualSyncPlan Push(
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<string>>
                transferAsync
        )
            => new(
                sync => sync.DiscoverLocalPaths(),
                PushStarting,
                SaveBackups.CloudBeforeManualPushAsync,
                PushBackedUpCloudFiles,
                transferAsync
            );

        internal static ManualSyncPlan Pull(
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<string>>
                transferAsync
        )
            => new(
                sync => sync.DiscoverCloudPaths(),
                PullStarting,
                SaveBackups.LocalBeforeManualPullAsync,
                PullBackedUpLocalFiles,
                transferAsync
            );

        internal async Task RunAsync(string accountName, string refreshToken)
        {
            var sync = CreateManualSyncContext(accountName, refreshToken);
            var paths = _discoverPaths(sync);
            PatchHelper.Log(_startingMessage(paths.Count));

            var backedUp = await _backupAsync(sync, paths);
            if (backedUp > 0)
                PatchHelper.Log(_backedUpMessage(backedUp));

            PatchHelper.Log(await _transferAsync(sync, paths));
        }
    }
}
