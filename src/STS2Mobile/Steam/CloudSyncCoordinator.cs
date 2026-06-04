using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// Stateless cloud sync coordinator: auto sync, manual push/pull, and save backups.
internal static partial class CloudSyncCoordinator
{
    private static bool _localBackupEnabled;

    private readonly struct CloudOperationTimeout
    {
        private readonly string _operation;
        private readonly int _timeoutMs;

        private CloudOperationTimeout(string operation, int timeoutMs)
        {
            _operation = operation;
            _timeoutMs = timeoutMs;
        }

        internal static CloudOperationTimeout For(string operation, int timeoutMs)
            => new(operation, timeoutMs);

        internal async Task WaitAsync(Task task)
        {
            var timeout = Task.Delay(_timeoutMs);
            var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
            if (winner == task)
                return;

            var operation = _operation;
            _ = task.ContinueWith(
                t => PatchHelper.Log(LateCompletionMessage(operation, t.Exception)),
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously
            );
            throw new TimeoutException(
                $"{_operation} timed out after {_timeoutMs}ms"
            );
        }

        private static string LateCompletionMessage(
            string operation,
            AggregateException exception
        )
            => "[Cloud] Late completion after timeout for "
                + $"'{operation}', result: {exception?.GetBaseException().Message}";
    }

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }

    private static async Task WriteLocalContentFromCloudAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        string path,
        string content,
        int timeoutMs
    )
    {
        var cloudTime = cloud.GetLastModifiedTime(path);
        var writeTask = local.WriteFileAsync(path, content);
        await CloudOperationTimeout
            .For($"WriteLocalFile {path}", timeoutMs)
            .WaitAsync(writeTask)
            .ConfigureAwait(false);
        await writeTask.ConfigureAwait(false);
        local.SetLastModifiedTime(path, cloudTime);
    }

    private static async Task<string> ReadCloudContentAsync(
        ICloudSaveStore cloud,
        string path,
        string operation,
        int timeoutMs
    )
    {
        var task = cloud.ReadFileAsync(path);
        await CloudOperationTimeout
            .For($"{operation} {path}", timeoutMs)
            .WaitAsync(task)
            .ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }
}
