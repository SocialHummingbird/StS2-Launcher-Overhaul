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

        internal int RunCloudBatch(Func<int> run)
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
        var paths = sync.DiscoverLocalPaths();
        PatchHelper.Log(PushStarting(paths.Count));

        var backedUp = await SaveBackups.CloudBeforeManualPushAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(PushBackedUpCloudFiles(backedUp));

        var count = RunManualPushUploads(sync, paths);
        PatchHelper.Log(PushComplete(count));
    }

    internal static async Task ManualPullAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = sync.DiscoverCloudPaths();
        PatchHelper.Log(PullStarting(paths.Count));

        var backedUp = await SaveBackups.LocalBeforeManualPullAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(PullBackedUpLocalFiles(backedUp));

        var counts = await RunManualPullDownloadsAsync(sync, paths);
        PatchHelper.Log(PullComplete(counts.Downloaded, counts.Skipped));
    }
}
