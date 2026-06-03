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

    private readonly struct ManualSyncPlan<TResult>
    {
        private ManualSyncPlan(
            Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
            Func<int, string> startingMessage,
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<int>> backUpAsync,
            Func<int, string> backedUpMessage,
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<TResult>> transferAsync,
            Func<TResult, string> completeMessage
        )
        {
            DiscoverPaths = discoverPaths;
            StartingMessage = startingMessage;
            BackUpAsync = backUpAsync;
            BackedUpMessage = backedUpMessage;
            TransferAsync = transferAsync;
            CompleteMessage = completeMessage;
        }

        private Func<ManualSyncContext, IReadOnlyCollection<string>> DiscoverPaths { get; }
        private Func<int, string> StartingMessage { get; }
        private Func<ManualSyncContext, IReadOnlyCollection<string>, Task<int>> BackUpAsync { get; }
        private Func<int, string> BackedUpMessage { get; }
        private Func<ManualSyncContext, IReadOnlyCollection<string>, Task<TResult>> TransferAsync { get; }
        private Func<TResult, string> CompleteMessage { get; }

        internal static ManualSyncPlan<TResult> Create(
            Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
            Func<int, string> startingMessage,
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<int>> backUpAsync,
            Func<int, string> backedUpMessage,
            Func<ManualSyncContext, IReadOnlyCollection<string>, Task<TResult>> transferAsync,
            Func<TResult, string> completeMessage
        )
            => new(
                discoverPaths,
                startingMessage,
                backUpAsync,
                backedUpMessage,
                transferAsync,
                completeMessage
            );

        internal async Task RunAsync(ManualSyncContext sync)
        {
            var paths = DiscoverPaths(sync);
            PatchHelper.Log(StartingMessage(paths.Count));

            var backedUp = await BackUpAsync(sync, paths);
            if (backedUp > 0)
                PatchHelper.Log(BackedUpMessage(backedUp));

            var result = await TransferAsync(sync, paths);
            PatchHelper.Log(CompleteMessage(result));
        }
    }

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

        internal static ManualSyncContext Create(
            ISaveStore local,
            ICloudSaveStore cloud,
            DateTime deadline
        )
            => new(local, cloud, deadline);

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
        return ManualSyncContext.Create(
            store.LocalStore,
            store.CloudStore,
            DateTime.UtcNow.AddMilliseconds(ManualSyncOverallTimeoutMs)
        );
    }

    internal static Task ManualPushAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualPushPlan()
        );

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualPullPlan()
        );

    private static ManualSyncPlan<int> ManualPushPlan()
        => ManualSyncPlan<int>.Create(
            sync => sync.DiscoverLocalPaths(),
            PushStarting,
            (sync, paths) => SaveBackups.CloudBeforeManualPushAsync(sync, paths),
            PushBackedUpCloudFiles,
            RunManualPushUploadsAsync,
            PushComplete
        );

    private static ManualSyncPlan<ManualPullResult> ManualPullPlan()
        => ManualSyncPlan<ManualPullResult>.Create(
            sync => sync.DiscoverCloudPaths(),
            PullStarting,
            (sync, paths) => SaveBackups.LocalBeforeManualPullAsync(sync, paths),
            PullBackedUpLocalFiles,
            RunManualPullDownloadsAsync,
            result => result.CompleteMessage()
        );

    private static async Task RunManualSyncAsync<TResult>(
        string accountName,
        string refreshToken,
        ManualSyncPlan<TResult> plan
    )
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        await plan.RunAsync(sync);
    }
}
