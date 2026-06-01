using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static void ProgressContent(string path, string content, string source)
        {
            var canonLowerPath = NormalizeSavePath(path).ToLowerInvariant();
            if (!IsProgressSavePath(canonLowerPath))
                return;

            try
            {
                var backup = TryWriteBackup(
                    canonLowerPath,
                    content,
                    source,
                    "progress.save"
                );
                if (!backup.Written)
                    return;

                var backupPath = backup.PathOrThrow();
                var backupDir = Path.GetDirectoryName(backupPath)
                    ?? AppPaths.ExternalSaveBackupsDir;
                PatchHelper.Log(SaveBackedUpTo(source, path, backupPath));
                PruneProgressBackups(backupDir);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BackupFailed(source, path, ex));
            }
        }

        private static bool IsProgressSave(string path)
            => IsProgressSavePath(NormalizeSavePath(path).ToLowerInvariant());

        private static bool IsProgressSavePath(string canonLowerPath)
            => canonLowerPath.Contains("progress")
                && canonLowerPath.EndsWith(".save");
    }
}
