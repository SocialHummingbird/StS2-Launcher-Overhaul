using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private const string BackupSourceCloudPrePush = "cloud-pre-push";
        private const string BackupSourceLocalPrePull = "local-pre-pull";

        private static async Task<int> CloudBeforeManualPushAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                try
                {
                    if (!IsImportantSave(path) || !sync.CloudFileExists(path))
                        continue;

                    PatchHelper.Log(PushBackingUpCloud(path));
                    var content = await sync.ReadCloudContentAsync(path, "ManualPush backup read");
                    if (SaveContent(path, content, BackupSourceCloudPrePush))
                        backedUp++;
                }
                catch (System.Exception ex)
                {
                    PatchHelper.Log(PushCloudBackupFailed(path, ex));
                }
            }

            return backedUp;
        }

        private static int LocalBeforeManualPull(
            ManualSyncContext sync,
            IEnumerable<string> paths
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                try
                {
                    if (!sync.LocalFileExists(path))
                        continue;

                    if (SaveContent(path, sync.ReadLocalFile(path), BackupSourceLocalPrePull))
                        backedUp++;
                }
                catch (System.Exception ex)
                {
                    PatchHelper.Log(PullLocalBackupFailed(path, ex));
                }
            }

            return backedUp;
        }

        private static bool IsImportantSave(string path)
        {
            var lower = NormalizeSavePath(path).ToLowerInvariant();
            return lower.Contains("progress.save")
                || lower.Contains("current_run")
                || lower.Contains("prefs");
        }
    }
}
