using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupWriteRequest
        {
            internal BackupWriteRequest(
                string path,
                string content,
                string source,
                string? fileNameOverride,
                Action<string> onWritten
            )
            {
                Path = path;
                Content = content;
                Source = source;
                FileNameOverride = fileNameOverride;
                OnWritten = onWritten;
            }

            internal string Path { get; }
            internal string Content { get; }
            internal string Source { get; }
            internal string? FileNameOverride { get; }
            internal Action<string> OnWritten { get; }
        }

        private static bool SaveContent(string path, string content, string source)
            => SaveBackup(new BackupWriteRequest(
                path,
                content,
                source,
                null,
                _ => PatchHelper.Log(SaveBackedUp(source, path))
            ));

        private static bool SaveBackup(BackupWriteRequest request)
        {
            try
            {
                var backupPath = TryWriteBackup(request);
                if (backupPath == null)
                    return false;

                request.OnWritten(backupPath);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BackupFailed(request.Source, request.Path, ex));
                return false;
            }
        }

        private static string? TryWriteBackup(BackupWriteRequest request)
        {
            if (ShouldSkipBackup(request.Content))
                return null;

            var canonPath = NormalizeSavePath(request.Path);
            var backupPath = BuildBackupPath(
                GetProfileDir(canonPath),
                request.FileNameOverride ?? Path.GetFileName(canonPath),
                request.Source
            );

            File.WriteAllText(backupPath, request.Content);
            return backupPath;
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
