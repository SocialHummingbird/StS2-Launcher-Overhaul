#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct ManualBackupPlan
        {
            internal ManualBackupPlan(
                BackupSource source,
                Func<
                    string,
                    CancellationToken,
                    ValueTask<string?>
                > readContentAsync,
                Action<string, Exception> logFailure
            )
            {
                Source = source;
                ReadContentAsync = readContentAsync;
                LogFailure = logFailure;
            }

            private BackupSource Source { get; }
            private Func<
                string,
                CancellationToken,
                ValueTask<string?>
            > ReadContentAsync { get; }
            private Action<string, Exception> LogFailure { get; }

            internal async Task<bool> TryBackupPathAsync(
                string path,
                CancellationToken cancellationToken
            )
            {
                try
                {
                    var read = await ReadContentAsync(
                        path,
                        cancellationToken
                    );
                    return read != null
                        && await SaveContentAsync(
                            path,
                            read,
                            Source,
                            cancellationToken
                        );
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogFailure(path, ex);
                    return false;
                }
            }
        }

        private static async Task<int> RunManualBackupsAsync(
            IEnumerable<string> paths,
            ManualBackupPlan plan,
            CancellationToken cancellationToken,
            Action<string>? reportStarted = null,
            Action<string, bool>? reportProcessed = null
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                reportStarted?.Invoke(path);
                var created = await plan.TryBackupPathAsync(
                    path,
                    cancellationToken
                );
                if (created)
                    backedUp++;
                reportProcessed?.Invoke(path, created);
            }

            return backedUp;
        }
    }
}
