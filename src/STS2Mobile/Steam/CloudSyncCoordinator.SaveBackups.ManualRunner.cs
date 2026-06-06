using System;
using System.Collections.Generic;
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
                Func<string, ValueTask<string?>> readContentAsync,
                Action<string, Exception> logFailure
            )
            {
                Source = source;
                ReadContentAsync = readContentAsync;
                LogFailure = logFailure;
            }

            private BackupSource Source { get; }
            private Func<string, ValueTask<string?>> ReadContentAsync { get; }
            private Action<string, Exception> LogFailure { get; }

            internal async Task<bool> TryBackupPathAsync(string path)
            {
                try
                {
                    var read = await ReadContentAsync(path);
                    return read != null && SaveContent(path, read, Source);
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
            ManualBackupPlan plan
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                if (await plan.TryBackupPathAsync(path))
                    backedUp++;
            }

            return backedUp;
        }
    }
}
