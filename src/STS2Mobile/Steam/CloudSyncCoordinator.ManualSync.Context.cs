using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct ManualSyncBudget
    {
        private ManualSyncBudget(DateTime deadline)
        {
            _deadline = deadline;
        }

        private readonly DateTime _deadline;

        internal static ManualSyncBudget StartingNow()
            => new(DateTime.UtcNow.AddMilliseconds(ManualSyncOverallTimeoutMs));

        internal bool Exceeded(string message)
        {
            if (DateTime.UtcNow <= _deadline)
                return false;

            PatchHelper.Log(message);
            return true;
        }
    }

    private readonly struct ManualSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;
        private readonly ManualSyncBudget _budget;

        internal ManualSyncContext(
            ISaveStore local,
            ICloudSaveStore cloud,
            ManualSyncBudget budget
        )
        {
            _local = local;
            _cloud = cloud;
            _budget = budget;
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
            => _budget.Exceeded(message);

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
            ManualSyncBudget.StartingNow()
        );
    }
}
