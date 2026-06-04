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

    private readonly struct ManualSyncPathResult
    {
        private ManualSyncPathResult(
            int queued,
            int downloaded,
            int skipped,
            bool stopAfterBudget
        )
        {
            Queued = queued;
            Downloaded = downloaded;
            Skipped = skipped;
            StopAfterBudget = stopAfterBudget;
        }

        internal int Queued { get; }
        internal int Downloaded { get; }
        internal int Skipped { get; }
        internal bool StopAfterBudget { get; }

        internal static ManualSyncPathResult Skipped()
            => new(0, 0, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult QueuedPath()
            => new(1, 0, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult DownloadedPath()
            => new(0, 1, 0, stopAfterBudget: false);

        internal static ManualSyncPathResult SkippedMissingCloud()
            => new(0, 0, 1, stopAfterBudget: false);

        internal static ManualSyncPathResult BudgetExceeded()
            => new(0, 0, 0, stopAfterBudget: true);
    }

    private readonly struct ManualSyncResultTotals
    {
        private ManualSyncResultTotals(int queued, int downloaded, int skipped)
        {
            Queued = queued;
            Downloaded = downloaded;
            Skipped = skipped;
        }

        private int Queued { get; }
        private int Downloaded { get; }
        private int Skipped { get; }

        internal static ManualSyncResultTotals Empty()
            => new(0, 0, 0);

        internal ManualSyncResultTotals Add(ManualSyncPathResult result)
            => new(
                Queued + result.Queued,
                Downloaded + result.Downloaded,
                Skipped + result.Skipped
            );

        internal string PushCompleteMessage()
            => PushComplete(Queued);

        internal string PullCompleteMessage()
            => PullComplete(Downloaded, Skipped);
    }

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
        => ManualPushPlan.RunAsync(accountName, refreshToken);

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => ManualPullPlan.RunAsync(accountName, refreshToken);
}
