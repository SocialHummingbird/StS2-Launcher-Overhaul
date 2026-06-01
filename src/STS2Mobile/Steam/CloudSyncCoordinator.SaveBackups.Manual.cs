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

        private readonly struct ManualBackupPlan
        {
            internal ManualBackupPlan(
                string source,
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync,
                Action<string, Exception> logFailure
            )
            {
                Source = source;
                ReadContentAsync = readContentAsync;
                LogFailure = logFailure;
            }

            internal string Source { get; }
            internal Func<ManualSyncContext, string, ValueTask<string?>> ReadContentAsync { get; }
            internal Action<string, Exception> LogFailure { get; }
        }

        internal static Task<int> CloudBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                sync,
                paths,
                new ManualBackupPlan(
                    BackupSourceCloudPrePush,
                    ReadCloudPrePushContentAsync,
                    (path, ex) => PatchHelper.Log(PushCloudBackupFailed(path, ex))
                )
            );

        internal static Task<int> LocalBeforeManualPullAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
            => RunManualBackupsAsync(
                sync,
                paths,
                new ManualBackupPlan(
                    BackupSourceLocalPrePull,
                    ReadLocalPrePullContentAsync,
                    (path, ex) => PatchHelper.Log(PullLocalBackupFailed(path, ex))
                )
            );

        private static async Task<int> RunManualBackupsAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths,
            ManualBackupPlan plan
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                try
                {
                    var read = await plan.ReadContentAsync(sync, path);
                    if (read == null)
                        continue;

                    if (SaveContent(path, read, plan.Source))
                        backedUp++;
                }
                catch (Exception ex)
                {
                    plan.LogFailure(path, ex);
                }
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

        private static bool IsImportantSave(string path)
        {
            var lower = NormalizeSavePath(path).ToLowerInvariant();
            return lower.Contains("progress.save")
                || lower.Contains("current_run")
                || lower.Contains("prefs");
        }
    }
}
