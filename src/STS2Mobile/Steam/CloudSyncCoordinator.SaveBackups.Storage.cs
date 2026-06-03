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

            private string? Path { get; }

            internal static BackupWriteResult Skipped()
                => new(path: null);

            internal static BackupWriteResult WrittenTo(string path)
                => new(path);

            internal bool TryGetPath(out string path)
            {
                path = Path ?? string.Empty;
                return Path != null;
            }
        }

        private static bool SaveContent(string path, string content, string source)
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
            string source,
            string? fileNameOverride,
            Action<string> onWritten
        )
        {
            try
            {
                var backup = TryWriteBackup(path, content, source, fileNameOverride);
                if (!backup.TryGetPath(out var backupPath))
                    return false;

                onWritten(backupPath);
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
