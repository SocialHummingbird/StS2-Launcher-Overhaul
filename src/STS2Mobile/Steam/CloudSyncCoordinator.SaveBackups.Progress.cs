using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static void ProgressContent(string path, string content, string source)
        {
            var canonLowerPath = NormalizeSavePathLower(path);
            if (!CloudSavePath.IsProgressSave(path))
                return;

            SaveBackup(new BackupWriteRequest(
                canonLowerPath,
                content,
                source,
                "progress.save",
                backupPath => CompleteProgressBackup(source, path, backupPath)
            ));
        }

        private static void CompleteProgressBackup(
            string source,
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
