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

        public static Task<int> CloudBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                sync,
                paths,
                BackupSourceCloudPrePush,
                ReadCloudPrePushContentAsync,
                (path, ex) => PatchHelper.Log(PushCloudBackupFailed(path, ex))
            );

        public static Task<int> LocalBeforeManualPullAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                sync,
                paths,
                BackupSourceLocalPrePull,
                ReadLocalPrePullContentAsync,
                (path, ex) => PatchHelper.Log(PullLocalBackupFailed(path, ex))
            );

        private static async Task<int> RunManualBackupsAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths,
            string source,
            Func<ManualSyncContext, string, Task<string?>> readContentAsync,
            Action<string, Exception> logFailure
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                try
                {
                    var read = await readContentAsync(sync, path);
                    if (read == null)
                        continue;

                    if (SaveContent(path, read, source))
                        backedUp++;
                }
                catch (Exception ex)
                {
                    logFailure(path, ex);
                }
            }

            return backedUp;
        }

        private static async Task<string?> ReadCloudPrePushContentAsync(
            ManualSyncContext sync,
            string path
        )
        {
            if (!IsImportantSave(path) || !sync.CloudFileExists(path))
                return null;

            PatchHelper.Log(PushBackingUpCloud(path));
            return await sync.ReadCloudContentAsync(path, ManualPushBackupReadOperation);
        }

        private static Task<string?> ReadLocalPrePullContentAsync(
            ManualSyncContext sync,
            string path
        )
            => Task.FromResult(sync.ReadLocalFile(path));

        private static bool IsImportantSave(string path)
        {
            var lower = NormalizeSavePath(path).ToLowerInvariant();
            return lower.Contains("progress.save")
                || lower.Contains("current_run")
                || lower.Contains("prefs");
        }
    }
}
