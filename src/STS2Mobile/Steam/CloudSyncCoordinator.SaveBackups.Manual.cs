using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private const string ManualPushBackupReadOperation = "ManualPush backup read";

        internal static async Task<int> BeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
        {
            var importantPaths = ImportantSavePaths(paths);
            var cloudBackups = await CloudBeforeManualPushAsync(sync, paths);
            var localBackups = await LocalBeforeManualPushAsync(sync, paths);
            EnforceManualPushBackupEvidence(
                sync,
                importantPaths,
                CloudImportantSaveCount(sync, importantPaths),
                cloudBackups,
                localBackups
            );
            return cloudBackups + localBackups;
        }

        private static System.Collections.Generic.IReadOnlyList<string> ImportantSavePaths(
            IEnumerable<string> paths
        )
        {
            var important = new System.Collections.Generic.List<string>();
            foreach (var path in paths)
            {
                if (IsImportantSave(path) && !important.Contains(path))
                    important.Add(path);
            }

            return important;
        }

        private static void EnforceManualPushBackupEvidence(
            ManualSyncContext sync,
            System.Collections.Generic.IReadOnlyCollection<string> importantPaths,
            int cloudImportantSaveCount,
            int cloudBackups,
            int localBackups
        )
        {
            if (!_localBackupEnabled)
                return;

            if (!AppPaths.HasStoragePermission())
                throw new InvalidOperationException("Manual Push blocked: local backup is enabled but backup storage permission is unavailable.");

            if (importantPaths.Count > 0 && localBackups < importantPaths.Count)
                throw new InvalidOperationException(
                    $"Manual Push blocked: local pre-Push backup evidence is incomplete for important Android saves ({localBackups}/{importantPaths.Count})."
                );

            if (cloudImportantSaveCount > 0 && cloudBackups < cloudImportantSaveCount)
                throw new InvalidOperationException(
                    $"Manual Push blocked: cloud pre-Push backup evidence is incomplete for existing important Steam Cloud saves ({cloudBackups}/{cloudImportantSaveCount})."
                );
        }

        private static int CloudImportantSaveCount(
            ManualSyncContext sync,
            IEnumerable<string> importantPaths
        )
        {
            var count = 0;
            foreach (var path in importantPaths)
            {
                if (sync.CloudFileExists(path))
                    count++;
            }

            return count;
        }

        internal static Task<int> CloudBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                paths,
                new ManualBackupPlan(
                    BackupSource.CloudPrePush,
                    path => ReadCloudPrePushContentAsync(sync, path),
                    LogPushCloudBackupFailure
                )
            );

        internal static Task<int> LocalBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                paths,
                new ManualBackupPlan(
                    BackupSource.LocalPrePush,
                    path => ReadLocalPrePushContentAsync(sync, path),
                    LogPushLocalBackupFailure
                )
            );

        internal static Task<int> LocalBeforeManualPullAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                paths,
                new ManualBackupPlan(
                    BackupSource.LocalPrePull,
                    path => ReadLocalPrePullContentAsync(sync, path),
                    LogPullLocalBackupFailure
                )
            );

        private static async ValueTask<string?> ReadCloudPrePushContentAsync(
            ManualSyncContext sync,
            string path
        )
        {
            if (!IsImportantSave(path) || !sync.CloudFileExists(path))
                return null;

            PatchHelper.Log(PushBackingUpCloud(path));
            return await sync.ReadCloudContentAsync(path, ManualPushBackupReadOperation);
        }

        private static ValueTask<string?> ReadLocalPrePushContentAsync(
            ManualSyncContext sync,
            string path
        )
            => new(IsImportantSave(path) ? sync.ReadLocalFile(path) : null);

        private static ValueTask<string?> ReadLocalPrePullContentAsync(
            ManualSyncContext sync,
            string path
        )
            => new(sync.ReadLocalFile(path));

        private static void LogPushCloudBackupFailure(string path, Exception ex)
            => PatchHelper.Log(PushCloudBackupFailed(path, ex));

        private static void LogPushLocalBackupFailure(string path, Exception ex)
            => PatchHelper.Log(PushLocalBackupFailed(path, ex));

        private static void LogPullLocalBackupFailure(string path, Exception ex)
            => PatchHelper.Log(PullLocalBackupFailed(path, ex));

        private static bool IsImportantSave(string path)
            => CloudSavePath.IsImportantForBackup(path);
    }
}
