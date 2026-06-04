using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupWriteRequest
        {
            private BackupWriteRequest(
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

            internal static BackupWriteRequest Standard(
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

            internal static BackupWriteRequest Progress(
                string path,
                string content,
                BackupSource source,
                Action<string> onWritten
            )
                => new(
                    path,
                    content,
                    source,
                    "progress.save",
                    onWritten
                );

            internal bool TrySave()
            {
                if (ShouldSkipBackup(Content))
                    return false;

                CreateTarget().Write();
                return true;
            }

            internal string FailureMessage(Exception ex)
                => BackupFailed(Source, Path, ex);

            private BackupWriteTarget CreateTarget()
                => new(
                    BuildBackupPathForSave(Path, FileNameOverride, Source),
                    Content,
                    OnWritten
                );
        }
    }
}
