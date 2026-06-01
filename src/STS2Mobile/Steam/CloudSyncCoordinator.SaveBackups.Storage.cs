using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupWriteResult
        {
            private BackupWriteResult(string? path)
            {
                Path = path;
            }

            internal string? Path { get; }
            internal bool Written => Path != null;

            internal static BackupWriteResult Skipped()
                => new(path: null);

            internal static BackupWriteResult WrittenTo(string path)
                => new(path);

            internal string PathOrThrow()
                => Path
                    ?? throw new InvalidOperationException("Backup was not written");
        }

        private static bool SaveContent(string path, string content, string source)
        {
            try
            {
                if (!TryWriteBackup(path, content, source, null).Written)
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

        private static BackupWriteResult TryWriteBackup(
            string path,
            string content,
            string source,
            string? fileNameOverride
        )
        {
            if (ShouldSkipBackup(content))
                return BackupWriteResult.Skipped();

            var canonPath = NormalizeSavePath(path);
            var backupPath = BuildBackupPath(
                GetProfileDir(canonPath),
                fileNameOverride ?? Path.GetFileName(canonPath),
                source
            );

            File.WriteAllText(backupPath, content);
            return BackupWriteResult.WrittenTo(backupPath);
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
