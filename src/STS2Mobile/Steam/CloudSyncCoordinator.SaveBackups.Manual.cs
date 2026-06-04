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

        private readonly struct ManualBackupPlan
        {
            private ManualBackupPlan(
                string source,
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync,
                Action<string, Exception> logFailure
            )
            {
                Source = source;
                ReadContentAsync = readContentAsync;
                LogFailure = logFailure;
            }

            private string Source { get; }
            private Func<ManualSyncContext, string, ValueTask<string?>> ReadContentAsync { get; }
            private Action<string, Exception> LogFailure { get; }

            internal static ManualBackupPlan CloudBeforeManualPush(
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync
            )
                => new(
                    BackupSourceCloudPrePush,
                    readContentAsync,
                    LogPushCloudBackupFailure
                );

            internal static ManualBackupPlan LocalBeforeManualPull(
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync
            )
                => new(
                    BackupSourceLocalPrePull,
                    readContentAsync,
                    LogPullLocalBackupFailure
                );

            internal async Task<bool> TryBackupAsync(ManualSyncContext sync, string path)
            {
                try
                {
                    var read = await ReadContentAsync(sync, path);
                    return read != null && SaveContent(path, read, Source);
                }
                catch (Exception ex)
                {
                    LogFailure(path, ex);
                    return false;
                }
            }
        }

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

        private static async Task<int> RunManualBackupsAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths,
            ManualBackupPlan plan
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                if (await plan.TryBackupAsync(sync, path))
                    backedUp++;
            }

            return backedUp;
        }

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
