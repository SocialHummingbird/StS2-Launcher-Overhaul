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

        public ManualSyncContext(
            ISaveStore local,
            ICloudSaveStore cloud,
            DateTime deadline
        )
        {
            _local = local;
            _cloud = cloud;
            _deadline = deadline;
        }

        public IReadOnlyCollection<string> DiscoverLocalPaths()
            => SavePathDiscovery.Get(_local);

        public IReadOnlyCollection<string> DiscoverCloudPaths()
            => SavePathDiscovery.Get(_cloud);

        public bool CloudFileExists(string path)
            => _cloud.FileExists(path);

        public string? ReadLocalFile(string path)
            => _local.FileExists(path) ? _local.ReadFile(path) : null;

        public void WriteCloudFile(string path, string content)
        {
            _cloud.WriteFile(path, content);
        }

        public Task<string> ReadCloudContentAsync(string path, string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                path,
                operation,
                ManualSyncPerPathTimeoutMs
            );

        public Task WriteCloudContentAsync(string path, string content)
            => CloudSyncCoordinator.WriteCloudContentAsync(
                _local,
                _cloud,
                path,
                content,
                ManualSyncPerPathTimeoutMs
            );

        public bool BudgetExceeded(string message)
        {
            if (DateTime.UtcNow <= _deadline)
                return false;

            PatchHelper.Log(message);
            return true;
        }

        public int RunCloudBatch(Func<int> run)
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

    internal static async Task ManualPushAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = await PrepareManualSyncAsync(
            sync.DiscoverLocalPaths(),
            PushStarting,
            SaveBackups.CloudBeforeManualPushAsync,
            PushBackedUpCloudFiles,
            sync
        );

        var count = RunManualPushUploads(sync, paths);
        PatchHelper.Log(PushComplete(count));
    }

    internal static async Task ManualPullAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = await PrepareManualSyncAsync(
            sync.DiscoverCloudPaths(),
            PullStarting,
            SaveBackups.LocalBeforeManualPullAsync,
            PullBackedUpLocalFiles,
            sync
        );

        var counts = await RunManualPullDownloadsAsync(sync, paths);
        PatchHelper.Log(PullComplete(counts.Downloaded, counts.Skipped));
    }

    private static async Task<IReadOnlyCollection<string>> PrepareManualSyncAsync(
        IReadOnlyCollection<string> paths,
        Func<int, string> startingMessage,
        Func<ManualSyncContext, IReadOnlyCollection<string>, Task<int>> backupAsync,
        Func<int, string> backedUpMessage,
        ManualSyncContext sync
    )
    {
        PatchHelper.Log(startingMessage(paths.Count));

        var backedUp = await backupAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(backedUpMessage(backedUp));

        return paths;
    }
}
