using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static bool SaveContent(string path, string content, string source)
        {
            try
            {
                if (TryWriteBackup(path, content, source, null) == null)
                    return false;

                PatchHelper.Log(SaveBackedUp(source, path));
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BackupFailed(source, path, ex));
                return false;
            }
        }

        private static string TryWriteBackup(
            string path,
            string content,
            string source,
            string? fileNameOverride
        )
        {
            if (ShouldSkipBackup(content))
                return null;

            var canonPath = NormalizeSavePath(path);
            var backupPath = BuildBackupPath(
                GetProfileDir(canonPath),
                fileNameOverride ?? Path.GetFileName(canonPath),
                source
            );

            File.WriteAllText(backupPath, content);
            return backupPath;
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
