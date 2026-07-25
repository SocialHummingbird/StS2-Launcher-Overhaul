#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
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

    private sealed class ManualSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;
        private readonly ManualSyncBudget _budget;
        private readonly CloudOperationProgressTracker _progress;
        private readonly CancellationToken _cancellationToken;

        internal ManualSyncContext(
            ISaveStore local,
            ICloudSaveStore cloud,
            ManualSyncBudget budget,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
        )
        {
            _local = local;
            _cloud = cloud;
            _budget = budget;
            _progress = progress;
            _cancellationToken = cancellationToken;
        }

        internal IReadOnlyCollection<string> DiscoverLocalPaths()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return SavePathDiscovery.Get(_local, _cancellationToken);
        }

        internal IReadOnlyCollection<string> DiscoverCloudPaths()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_cloud is ICancellableCloudMetadataStore metadata)
            {
                metadata.PrepareFileMetadata(_cancellationToken);
            }

            return SavePathDiscovery.Get(_cloud, _cancellationToken);
        }

        internal bool CloudFileExists(string path)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _cloud.FileExists(path);
        }

        internal async ValueTask<string?> ReadLocalFileAsync(string path)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (!_local.FileExists(path))
                return null;

            return await CancellableSaveStore.ReadFileAsync(
                _local,
                path,
                _cancellationToken
            ).ConfigureAwait(false);
        }

        internal async Task WriteLocalContentAsync(string path, string content)
        {
            await WaitForCloudOperationAsync(
                $"WriteLocalFile {path}",
                ManualSyncPerPathTimeoutMs,
                token => CancellableSaveStore.WriteFileAsync(
                    _local,
                    path,
                    content,
                    token
                ),
                _cancellationToken
            ).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();
            PatchHelper.Log($"[Cloud] Local write path: {path} -> {_local.GetFullPath(path)}");
        }

        internal Task WriteCloudFileAsync(string path, string content)
        {
            return WaitForCloudOperationAsync(
                $"WriteCloudFile {path}",
                ManualSyncPerPathTimeoutMs,
                token => CancellableSaveStore.WriteFileAsync(
                    _cloud,
                    path,
                    content,
                    token
                ),
                _cancellationToken
            );
        }

        internal Task<string> ReadCloudContentAsync(string path, string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                path,
                operation,
                ManualSyncPerPathTimeoutMs,
                _cancellationToken
            );

        internal Task WriteLocalContentFromCloudAsync(string path, string content)
            => CloudSyncCoordinator.WriteLocalContentFromCloudAsync(
                _local,
                _cloud,
                path,
                content,
                ManualSyncPerPathTimeoutMs,
                _cancellationToken
            );

        internal bool BudgetExceeded(string message)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _budget.Exceeded(message);
        }

        internal CancellationToken CancellationToken
            => _cancellationToken;

        internal void ReportEnumerationStarted()
            => _progress.EnumerationStarted(
                "Checking Steam Cloud save locations"
            );

        internal void ReportEnumerationCompleted(int pathCount)
            => _progress.EnumerationCompleted(pathCount);

        internal void ReportBackupStarted(int totalCount)
            => _progress.BackupStarted(totalCount);

        internal void ReportBackupProcessed(string path, bool created)
            => _progress.BackupProcessed(path, created);

        internal void ReportBackupPathStarted(string path)
            => _progress.BackupPathStarted(path);

        internal void ReportProfilePreparationStarted(int totalCount)
            => _progress.ProfilePreparationStarted(totalCount);

        internal void ReportProfilePreparationProcessed(
            string path,
            bool backupCreated
        )
            => _progress.ProfilePreparationProcessed(path, backupCreated);

        internal void ReportProfilePreparationPathStarted(string path)
            => _progress.ProfilePreparationPathStarted(path);

        internal void ReportTransferStarted(int totalCount)
            => _progress.TransferStarted(totalCount);

        internal void ReportTransferProcessed(
            string path,
            CloudTransferPathOutcome outcome
        )
            => _progress.TransferProcessed(path, outcome);

        internal void ReportTransferPathStarted(string path)
            => _progress.TransferPathStarted(path);

        internal void ReportProfileSeedingStarted(int totalCount)
            => _progress.ProfileSeedingStarted(totalCount);

        internal void ReportProfileSeedProcessed(string path, bool seeded)
            => _progress.ProfileSeedProcessed(path, seeded);

        internal void ReportProfileSeedPathStarted(string path)
            => _progress.ProfileSeedPathStarted(path);

        internal void ReportFinalizing(string currentItem)
            => _progress.Finalizing(currentItem);

        internal LocalBackupRefreshResult RefreshLocalBackupMirror()
            => SaveBackups.RefreshLocalMirror(
                _local,
                restoreMissing: false,
                _cancellationToken
            );

        internal CloudOperationState ProgressState
            => _progress.State;

    }

    private static ManualSyncContext CreateManualSyncContext(
        string accountName,
        string refreshToken,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        var store = CloudSaveStoreFactory.CreateCloudSaveStore(accountName, refreshToken);
        return CreateManualSyncContext(
            store.LocalStore,
            store.CloudStore,
            progress,
            cancellationToken
        );
    }

    private static ManualSyncContext CreateManualSyncContext(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        return new ManualSyncContext(
            local,
            cloud,
            ManualSyncBudget.StartingNow(),
            progress,
            cancellationToken
        );
    }
}
