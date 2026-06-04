using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static bool SaveContent(
            string path,
            string content,
            BackupSource source
        )
            => SaveBackup(
                path,
                content,
                source,
                fileNameOverride: null,
                _ => PatchHelper.Log(SaveBackedUp(source, path))
            );

        private static bool SaveBackup(
            string path,
            string content,
            BackupSource source,
            string? fileNameOverride,
            Action<string> onWritten
        )
        {
            if (ShouldSkipBackup(content))
                return false;

            try
            {
                var backupPath = BuildBackupPathForSave(
                    path,
                    fileNameOverride,
                    source
                );
                File.WriteAllText(backupPath, content);
                onWritten(backupPath);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BackupFailed(source, path, ex));
                return false;
            }
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
