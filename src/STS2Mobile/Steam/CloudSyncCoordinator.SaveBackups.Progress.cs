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
            => IsProgressSavePath(NormalizeSavePath(path).ToLowerInvariant());

        private static bool IsProgressSavePath(string canonLowerPath)
            => canonLowerPath.Contains("progress")
                && canonLowerPath.EndsWith(".save");
    }
}
