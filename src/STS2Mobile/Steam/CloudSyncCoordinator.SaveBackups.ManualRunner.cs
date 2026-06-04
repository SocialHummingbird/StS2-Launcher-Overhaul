using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static async Task<int> RunManualBackupsAsync(
            IEnumerable<string> paths,
            BackupSource source,
            Func<string, ValueTask<string?>> readContentAsync,
            Action<string, Exception> logFailure
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                if (await TryBackupManualPathAsync(
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
            string path,
            BackupSource source,
            Func<string, ValueTask<string?>> readContentAsync,
            Action<string, Exception> logFailure
        )
        {
            try
            {
                var read = await readContentAsync(path);
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
