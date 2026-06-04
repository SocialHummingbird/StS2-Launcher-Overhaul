using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct ManualBackupPlan
        {
            private ManualBackupPlan(
                string source,
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync,
                Action<string, Exception> logFailure
            )
            {
                Source = source;
                ReadContentAsync = readContentAsync;
                LogFailure = logFailure;
            }

            private string Source { get; }
            private Func<ManualSyncContext, string, ValueTask<string?>> ReadContentAsync { get; }
            private Action<string, Exception> LogFailure { get; }

            internal static ManualBackupPlan CloudBeforeManualPush(
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync
            )
                => new(
                    BackupSourceCloudPrePush,
                    readContentAsync,
                    LogPushCloudBackupFailure
                );

            internal static ManualBackupPlan LocalBeforeManualPull(
                Func<ManualSyncContext, string, ValueTask<string?>> readContentAsync
            )
                => new(
                    BackupSourceLocalPrePull,
                    readContentAsync,
                    LogPullLocalBackupFailure
                );

            internal async Task<bool> TryBackupAsync(
                ManualSyncContext sync,
                string path
            )
            {
                try
                {
                    var read = await ReadContentAsync(sync, path);
                    return read != null && SaveContent(path, read, Source);
                }
                catch (Exception ex)
                {
                    LogFailure(path, ex);
                    return false;
                }
            }
        }
    }
}
