using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private const string BackupSourceCloudPrePush = "cloud-pre-push";
        private const string BackupSourceLocalPrePull = "local-pre-pull";
        private const string ManualPushBackupReadOperation = "ManualPush backup read";

        private static readonly ManualBackupPlan CloudBeforeManualPushPlan =
            ManualBackupPlan.CloudBeforeManualPush(ReadCloudPrePushContentAsync);

        private static readonly ManualBackupPlan LocalBeforeManualPullPlan =
            ManualBackupPlan.LocalBeforeManualPull(ReadLocalPrePullContentAsync);

        internal static Task<int> CloudBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(sync, paths, CloudBeforeManualPushPlan);

        internal static Task<int> LocalBeforeManualPullAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(sync, paths, LocalBeforeManualPullPlan);

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

        private static ValueTask<string?> ReadLocalPrePullContentAsync(
            ManualSyncContext sync,
            string path
        )
            => new(sync.ReadLocalFile(path));

        private static void LogPushCloudBackupFailure(string path, Exception ex)
            => PatchHelper.Log(PushCloudBackupFailed(path, ex));

        private static void LogPullLocalBackupFailure(string path, Exception ex)
            => PatchHelper.Log(PullLocalBackupFailed(path, ex));

        private static bool IsImportantSave(string path)
            => CloudSavePath.IsImportantForBackup(path);
    }
}
