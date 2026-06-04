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

            private string Path { get; }
            private string Content { get; }
            private string Source { get; }
            private string? FileNameOverride { get; }
            private Action<string> OnWritten { get; }

            internal string? TryWrite()
            {
                if (ShouldSkipBackup(Content))
                    return null;

                var canonPath = NormalizeSavePath(Path);
                var backupPath = BuildBackupPath(
                    GetProfileDir(canonPath),
                    FileNameOverride ?? System.IO.Path.GetFileName(canonPath),
                    Source
                );

                File.WriteAllText(backupPath, Content);
                return backupPath;
            }

            internal void NotifyWritten(string backupPath)
            {
                OnWritten(backupPath);
            }

            internal string FailureMessage(Exception ex)
                => BackupFailed(Source, Path, ex);
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
                var backupPath = request.TryWrite();
                if (backupPath == null)
                    return false;

                request.NotifyWritten(backupPath);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(request.FailureMessage(ex));
                return false;
            }
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
