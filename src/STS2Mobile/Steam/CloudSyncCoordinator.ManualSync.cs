using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const string ManualPullDownloadOperation = "ManualPull download";

    private readonly struct ManualSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;
        private readonly DateTime _deadline;

        internal ManualSyncContext(
            ISaveStore local,
            ICloudSaveStore cloud,
            DateTime deadline
        )
        {
            _local = local;
            _cloud = cloud;
            _deadline = deadline;
        }

        internal IReadOnlyCollection<string> DiscoverLocalPaths()
            => SavePathDiscovery.Get(_local);

        internal IReadOnlyCollection<string> DiscoverCloudPaths()
            => SavePathDiscovery.Get(_cloud);

        internal bool CloudFileExists(string path)
            => _cloud.FileExists(path);

        internal string? ReadLocalFile(string path)
            => _local.FileExists(path) ? _local.ReadFile(path) : null;

        internal void WriteCloudFile(string path, string content)
        {
            _cloud.WriteFile(path, content);
        }

        internal Task<string> ReadCloudContentAsync(string path, string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                path,
                operation,
                ManualSyncPerPathTimeoutMs
            );

        internal Task WriteLocalContentFromCloudAsync(string path, string content)
            => CloudSyncCoordinator.WriteLocalContentFromCloudAsync(
                _local,
                _cloud,
                path,
                content,
                ManualSyncPerPathTimeoutMs
            );

        internal bool BudgetExceeded(string message)
        {
            if (DateTime.UtcNow <= _deadline)
                return false;

            PatchHelper.Log(message);
            return true;
        }

        internal TResult RunCloudBatch<TResult>(Func<TResult> run)
        {
            _cloud.BeginSaveBatch();
            try
            {
                return run();
            }
            finally
            {
                _cloud.EndSaveBatch();
            }
        }
    }

    private static ManualSyncContext CreateManualSyncContext(
        string accountName,
        string refreshToken
    )
    {
        var store = CloudSaveStoreFactory.CreateCloudSaveStore(accountName, refreshToken);
        return new ManualSyncContext(
            store.LocalStore,
            store.CloudStore,
            DateTime.UtcNow.AddMilliseconds(ManualSyncOverallTimeoutMs)
        );
    }

    internal static Task ManualPushAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            sync => sync.DiscoverLocalPaths(),
            PushStarting,
            SaveBackups.CloudBeforeManualPushAsync,
            PushBackedUpCloudFiles,
            RunManualPushUploadsAsync
        );

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            sync => sync.DiscoverCloudPaths(),
            PullStarting,
            SaveBackups.LocalBeforeManualPullAsync,
            PullBackedUpLocalFiles,
            RunManualPullDownloadsAsync
        );

    private static async Task RunManualSyncAsync(
        string accountName,
        string refreshToken,
        Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
        Func<int, string> startingMessage,
        Func<ManualSyncContext, IEnumerable<string>, Task<int>> backupAsync,
        Func<int, string> backedUpMessage,
        Func<ManualSyncContext, IReadOnlyCollection<string>, Task<string>> transferAsync
    )
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = discoverPaths(sync);
        PatchHelper.Log(startingMessage(paths.Count));

        var backedUp = await backupAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(backedUpMessage(backedUp));

        PatchHelper.Log(await transferAsync(sync, paths));
    }
}
