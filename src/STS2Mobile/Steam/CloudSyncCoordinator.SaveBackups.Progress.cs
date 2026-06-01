using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static void ProgressContent(string path, string content, string source)
        {
            var canonPath = NormalizeSavePath(path).ToLowerInvariant();
            if (!IsProgressSave(canonPath))
                return;

            try
            {
                var backupPath = TryWriteBackup(canonPath, content, source, "progress.save");
                if (backupPath == null)
                    return;

                var backupDir = Path.GetDirectoryName(backupPath) ?? AppPaths.ExternalSaveBackupsDir;
                PatchHelper.Log(SaveBackedUpTo(source, path, backupPath));
                PruneProgressBackups(backupDir);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BackupFailed(source, path, ex));
            }
        }

        private static bool IsProgressSave(string path)
        {
            var canonPath = NormalizeSavePath(path).ToLowerInvariant();
            return canonPath.Contains("progress") && canonPath.EndsWith(".save");
        }
    }
}
