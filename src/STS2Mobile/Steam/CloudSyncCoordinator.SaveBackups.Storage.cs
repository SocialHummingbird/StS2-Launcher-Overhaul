using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupWrite
        {
            private BackupWrite(
                string path,
                string content,
                BackupSource source,
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
            private BackupSource Source { get; }
            private string? FileNameOverride { get; }
            private Action<string> OnWritten { get; }

            private bool CanWrite()
                => BackupStorageAccess.CanWrite(Content);

            internal static BackupWrite Standard(
                string path,
                string content,
                BackupSource source
            )
                => new(
                    path,
                    content,
                    source,
                    fileNameOverride: null,
                    _ => PatchHelper.Log(SaveBackedUp(source, path))
                );

            internal static BackupWrite Progress(
                string path,
                string content,
                BackupSource source,
                Action<string> onWritten
            )
                => new(
                    path,
                    content,
                    source,
                    fileNameOverride: "progress.save",
                    onWritten
                );

            internal bool TrySave()
            {
                if (!CanWrite())
                    return false;

                try
                {
                    var backupPath = BuildBackupPath();
                    File.WriteAllText(backupPath, Content);
                    OnWritten(backupPath);
                    return true;
                }
                catch (Exception ex)
                {
                    LogFailure(ex);
                    return false;
                }
            }

            private void LogFailure(Exception ex)
                => PatchHelper.Log(BackupFailed(Source, Path, ex));

            private string BuildBackupPath()
                => BuildBackupPathForSave(
                    Path,
                    FileNameOverride,
                    Source
                );
        }

        private readonly struct BackupStorageAccess
        {
            internal static bool CanWrite(string content)
                => !string.IsNullOrEmpty(content) && HasBackupAccess();

            private static bool HasBackupAccess()
                => _localBackupEnabled && AppPaths.HasStoragePermission();
        }

        private static bool SaveContent(
            string path,
            string content,
            BackupSource source
        )
            => BackupWrite.Standard(path, content, source).TrySave();
    }
}
