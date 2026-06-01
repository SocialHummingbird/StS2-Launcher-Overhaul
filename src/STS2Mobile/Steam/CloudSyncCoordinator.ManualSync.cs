using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;

    private readonly struct ManualSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;
        private readonly DateTime _deadline;

        private ManualSyncContext(
            ISaveStore local,
            ICloudSaveStore cloud,
            DateTime deadline
        )
        {
            _local = local;
            _cloud = cloud;
            _deadline = deadline;
        }

        private IReadOnlyCollection<string> DiscoverLocalPaths()
            => SavePathDiscovery.Get(_local);

        private IReadOnlyCollection<string> DiscoverCloudPaths()
            => SavePathDiscovery.Get(_cloud);

        private void BeginCloudBatch()
        {
            _cloud.BeginSaveBatch();
        }

        private void EndCloudBatch()
        {
            _cloud.EndSaveBatch();
        }

        private bool LocalFileExists(string path)
            => _local.FileExists(path);

        private bool CloudFileExists(string path)
            => _cloud.FileExists(path);

        private string ReadLocalFile(string path)
            => _local.ReadFile(path);

        private void WriteCloudFile(string path, string content)
        {
            _cloud.WriteFile(path, content);
        }

        private Task<string> ReadCloudContentAsync(string path, string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                path,
                operation,
                ManualSyncPerPathTimeoutMs
            );

        private Task WriteCloudContentAsync(string path, string content)
            => CloudSyncCoordinator.WriteCloudContentAsync(
                _local,
                _cloud,
                path,
                content,
                ManualSyncPerPathTimeoutMs
            );

        private bool BudgetExceeded(string message)
        {
            if (DateTime.UtcNow <= _deadline)
                return false;

            PatchHelper.Log(message);
            return true;
        }
    }

    private readonly struct ManualPullCounts
    {
        private ManualPullCounts(int downloaded, int skipped)
        {
            Downloaded = downloaded;
            Skipped = skipped;
        }

        private int Downloaded { get; }
        private int Skipped { get; }
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
        var paths = await PrepareManualPushAsync(sync);

        var count = ManualPushBatch.Run(sync, paths);
        PatchHelper.Log(PushComplete(count));
    }

    internal static async Task ManualPullAllAsync(string accountName, string refreshToken)
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = PrepareManualPull(sync);

        var counts = await ManualPullBatch.RunAsync(sync, paths);
        PatchHelper.Log(PullComplete(counts.Downloaded, counts.Skipped));
    }

    private static async Task<IReadOnlyCollection<string>> PrepareManualPushAsync(
        ManualSyncContext sync
    )
    {
        var paths = sync.DiscoverLocalPaths();
        PatchHelper.Log(PushStarting(paths.Count));

        var backedUp = await SaveBackups.CloudBeforeManualPushAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(PushBackedUpCloudFiles(backedUp));

        return paths;
    }

    private static IReadOnlyCollection<string> PrepareManualPull(ManualSyncContext sync)
    {
        var paths = sync.DiscoverCloudPaths();
        PatchHelper.Log(PullStarting(paths.Count));

        var backedUp = SaveBackups.LocalBeforeManualPull(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(PullBackedUpLocalFiles(backedUp));

        return paths;
    }
}
