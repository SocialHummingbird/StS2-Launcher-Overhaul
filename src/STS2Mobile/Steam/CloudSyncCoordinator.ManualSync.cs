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

        internal Func<ManualSyncContext, IReadOnlyCollection<string>> DiscoverPaths { get; }
        internal Func<int, string> StartingMessage { get; }
        internal Func<ManualSyncContext, IReadOnlyCollection<string>, Task<int>> BackUpAsync { get; }
        internal Func<int, string> BackedUpMessage { get; }
        internal Func<ManualSyncContext, IReadOnlyCollection<string>, Task<TResult>> TransferAsync { get; }
        internal Func<TResult, string> CompleteMessage { get; }

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
            ManualSyncPlan<int>.Create(
                sync => sync.DiscoverLocalPaths(),
                PushStarting,
                (sync, paths) => SaveBackups.CloudBeforeManualPushAsync(sync, paths),
                PushBackedUpCloudFiles,
                (sync, paths) => Task.FromResult(RunManualPushUploads(sync, paths)),
                PushComplete
            )
        );

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualSyncPlan<ManualPullResult>.Create(
                sync => sync.DiscoverCloudPaths(),
                PullStarting,
                (sync, paths) => SaveBackups.LocalBeforeManualPullAsync(sync, paths),
                PullBackedUpLocalFiles,
                RunManualPullDownloadsAsync,
                result => PullComplete(result.Downloaded, result.Skipped)
            )
        );

    private static async Task RunManualSyncAsync<TResult>(
        string accountName,
        string refreshToken,
        ManualSyncPlan<TResult> plan
    )
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = plan.DiscoverPaths(sync);
        PatchHelper.Log(plan.StartingMessage(paths.Count));

        var backedUp = await plan.BackUpAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(plan.BackedUpMessage(backedUp));

        var result = await plan.TransferAsync(sync, paths);
        PatchHelper.Log(plan.CompleteMessage(result));
    }
}
