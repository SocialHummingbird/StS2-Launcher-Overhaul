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

        internal void ThrowIfExceeded()
        {
            if (DateTime.UtcNow > _deadline)
            {
                throw new TimeoutException(
                    $"Save transfer exceeded {ManualSyncOverallTimeoutMs}ms"
                );
            }
        }
    }

    private sealed class ManualSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;
        private readonly ITransferSaveStore _cloudTransfer;
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
            _cloudTransfer = cloud as ITransferSaveStore
                ?? throw new InvalidOperationException(
                    "Steam Cloud store does not support verified save transfer"
                );
            _budget = budget;
            _progress = progress;
            _cancellationToken = cancellationToken;
        }

        internal Task<ulong> AuthenticateAsync()
            => WaitAsync(
                "Steam authentication",
                token => _cloudTransfer.GetAuthenticatedSteamId64Async(token)
            );

        internal void PrepareCloudMetadata()
        {
            Checkpoint();
            if (_cloud is not ICancellableCloudMetadataStore metadata)
            {
                throw new InvalidOperationException(
                    "Steam Cloud store does not support reliable file listing"
                );
            }

            metadata.PrepareFileMetadata(_cancellationToken);
            Checkpoint();
        }

        internal IReadOnlyList<string> GetSourceHistoryFiles(
            CloudOperationKind direction,
            string directory
        )
            => GetHistoryFiles(
                direction == CloudOperationKind.Push ? _local : _cloud,
                directory
            );

        internal IReadOnlyList<string> GetDestinationHistoryFiles(
            CloudOperationKind direction,
            string directory
        )
            => GetHistoryFiles(
                direction == CloudOperationKind.Push ? _cloud : _local,
                directory
            );

        private IReadOnlyList<string> GetHistoryFiles(
            ISaveStore store,
            string directory
        )
        {
            Checkpoint();
            if (!store.DirectoryExists(directory))
                return Array.Empty<string>();

            var files = store.GetFilesInDirectory(directory);
            Checkpoint();
            return files;
        }

        internal Task<bool> SourceFileExistsAsync(
            CloudOperationKind direction,
            string path
        )
            => direction == CloudOperationKind.Push
                ? LocalFileExistsAsync(path)
                : RemoteFileExistsAsync(path);

        internal Task<byte[]> ReadSourceFileBytesAsync(
            CloudOperationKind direction,
            string path
        )
            => direction == CloudOperationKind.Push
                ? ReadLocalFileBytesAsync(path)
                : ReadRemoteFileBytesForVerificationAsync(path);

        internal Task<bool> DestinationFileExistsAsync(
            CloudOperationKind direction,
            string path
        )
            => direction == CloudOperationKind.Push
                ? RemoteFileExistsAsync(path)
                : LocalFileExistsAsync(path);

        internal Task<byte[]> ReadDestinationFileBytesAsync(
            CloudOperationKind direction,
            string path
        )
            => direction == CloudOperationKind.Push
                ? ReadRemoteFileBytesForVerificationAsync(path)
                : ReadLocalFileBytesAsync(path);

        internal Task WriteDestinationFileBytesAsync(
            CloudOperationKind direction,
            string path,
            byte[] content
        )
            => direction == CloudOperationKind.Push
                ? WriteRemoteFileBytesAsync(path, content)
                : WriteLocalFileBytesAsync(path, content);

        internal Task DeleteDestinationFileAsync(
            CloudOperationKind direction,
            string path
        )
            => direction == CloudOperationKind.Push
                ? DeleteRemoteFileAsync(path)
                : DeleteLocalFileAsync(path);

        internal Task<string> ReadRemoteFileForVerificationAsync(string path)
            => WaitAsync(
                $"Read and verify Steam Cloud file {path}",
                token => _cloudTransfer.ReadFileForVerificationAsync(path, token)
            );

        internal Task<byte[]> ReadRemoteFileBytesForVerificationAsync(
            string path
        )
            => WaitAsync(
                $"Read and verify raw Steam Cloud file {path}",
                token => _cloudTransfer.ReadFileBytesForVerificationAsync(
                    path,
                    token
                )
            );

        internal Task<bool> RemoteFileExistsAsync(string path)
            => WaitAsync(
                $"Verify Steam Cloud file existence {path}",
                token => _cloudTransfer.FileExistsForVerificationAsync(path, token)
            );

        internal Task WriteRemoteFileAsync(string path, string content)
            => WaitAsync(
                $"Write Steam Cloud file {path}",
                token => CancellableSaveStore.WriteFileAsync(
                    _cloud,
                    path,
                    content,
                    token
                )
            );

        internal Task WriteRemoteFileBytesAsync(string path, byte[] content)
            => WaitAsync(
                $"Write raw Steam Cloud file {path}",
                token => CancellableSaveStore.WriteBytesAsync(
                    _cloud,
                    path,
                    content,
                    token
                )
            );

        internal Task DeleteRemoteFileAsync(string path)
            => WaitAsync(
                $"Delete Steam Cloud file {path}",
                token => _cloudTransfer.DeleteFileAsync(path, token)
            );

        internal Task<string> ReadLocalFileAsync(string path)
            => WaitAsync(
                $"Read local save {path}",
                token => CancellableSaveStore.ReadFileAsync(
                    _local,
                    path,
                    token
                )
            );

        internal Task<byte[]> ReadLocalFileBytesAsync(string path)
            => WaitAsync(
                $"Read raw local save {path}",
                token => CancellableSaveStore.ReadBytesAsync(
                    _local,
                    path,
                    token
                )
            );

        internal Task WriteLocalFileAsync(string path, string content)
            => WaitAsync(
                $"Write local save {path}",
                token => CancellableSaveStore.WriteFileAsync(
                    _local,
                    path,
                    content,
                    token
                )
            );

        internal Task WriteLocalFileBytesAsync(string path, byte[] content)
            => WaitAsync(
                $"Write raw local save {path}",
                token => CancellableSaveStore.WriteBytesAsync(
                    _local,
                    path,
                    content,
                    token
                )
            );

        internal Task DeleteLocalFileAsync(string path)
            => WaitAsync(
                $"Delete local save {path}",
                token => CancellableSaveStore.DeleteFileAsync(
                    _local,
                    path,
                    token
                )
            );

        internal Task<bool> LocalFileExistsAsync(string path)
        {
            Checkpoint();
            var exists = _local.FileExists(path);
            Checkpoint();
            return Task.FromResult(exists);
        }

        internal void Checkpoint()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _budget.ThrowIfExceeded();
        }

        internal void ReportEnumerationStarted(string message)
            => _progress.EnumerationStarted(message);

        internal void ReportEnumerationCompleted(int pathCount)
            => _progress.EnumerationCompleted(pathCount);

        internal void ReportBackupStarted(int totalCount)
            => _progress.BackupStarted(totalCount);

        internal void ReportBackupPathStarted(string path)
            => _progress.BackupPathStarted(path);

        internal void ReportBackupProcessed(string path, bool created)
            => _progress.BackupProcessed(path, created);

        internal void ReportTransferStarted(int totalCount)
            => _progress.TransferStarted(totalCount);

        internal void ReportTransferPathStarted(string path)
            => _progress.TransferPathStarted(path);

        internal void ReportTransferProcessed(string path)
            => _progress.TransferProcessed(path);

        internal void ReportFinalizing(string message)
            => _progress.Finalizing(message);

        internal CloudOperationState ProgressState
            => _progress.State;

        internal CancellationToken CancellationToken
            => _cancellationToken;

        internal ISaveStore LocalStore
            => _local;

        private Task WaitAsync(
            string operation,
            Func<CancellationToken, Task> run
        )
        {
            Checkpoint();
            return WaitForCloudOperationAsync(
                operation,
                ManualSyncPerPathTimeoutMs,
                run,
                _cancellationToken
            );
        }

        private Task<T> WaitAsync<T>(
            string operation,
            Func<CancellationToken, Task<T>> run
        )
        {
            Checkpoint();
            return WaitForCloudOperationAsync(
                operation,
                ManualSyncPerPathTimeoutMs,
                run,
                _cancellationToken
            );
        }
    }
}
