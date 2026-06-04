using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static async Task<int> RunManualBackupsAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths,
            BackupSource source,
            Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync,
            Action<string, Exception> logFailure
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                if (await TryBackupManualPathAsync(
                    sync,
                    path,
                    source,
                    readContentAsync,
                    logFailure
                ))
                    backedUp++;
            }

            return backedUp;
        }

        private static async Task<bool> TryBackupManualPathAsync(
            ManualSyncContext sync,
            string path,
            BackupSource source,
            Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync,
            Action<string, Exception> logFailure
        )
        {
            try
            {
                var read = await readContentAsync(sync, path);
                return read != null && SaveContent(path, read, source);
            }
            catch (Exception ex)
            {
                logFailure(path, ex);
                return false;
            }
        }
    }
}
