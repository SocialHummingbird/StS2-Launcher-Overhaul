using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static void ProgressContent(
            string path,
            string content,
            BackupSource source
        )
        {
            if (!IsProgressSave(path))
                return;

            var canonLowerPath = NormalizeSavePathLower(path);
            BackupWrite
                .Progress(
                    canonLowerPath,
                    content,
                    source,
                    backupPath => CompleteProgressBackup(source, path, backupPath)
                )
                .TrySave();
        }

        private static void CompleteProgressBackup(
            BackupSource source,
            string path,
            string backupPath
        )
        {
            var backupDir = Path.GetDirectoryName(backupPath)
                ?? AppPaths.ExternalSaveBackupsDir;
            PatchHelper.Log(SaveBackedUpTo(source, path, backupPath));
            PruneProgressBackups(backupDir);
        }

        private static bool IsProgressSave(string path)
            => CloudSavePath.IsProgressSave(path);
    }
}
