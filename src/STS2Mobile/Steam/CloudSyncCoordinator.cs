using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// Stateless cloud sync coordinator: auto sync, manual push/pull, and save backups.
internal static partial class CloudSyncCoordinator
{
    private static bool _localBackupEnabled;

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }

    private static async Task WriteCloudContentAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        string path,
        string content,
        int timeoutMs
    )
    {
        var cloudTime = cloud.GetLastModifiedTime(path);
        var writeTask = local.WriteFileAsync(path, content);
        await WaitForTimeoutAsync(writeTask, $"WriteLocalFile {path}", timeoutMs)
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
        await WaitForTimeoutAsync(task, $"{operation} {path}", timeoutMs)
            .ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }

    private static async Task WaitForTimeoutAsync(Task task, string operation, int timeoutMs)
    {
        var timeout = Task.Delay(timeoutMs);
        var winner = await Task.WhenAny(task, timeout).ConfigureAwait(false);
        if (winner == task)
            return;

        _ = task.ContinueWith(
            t =>
                PatchHelper.Log(LateCompletionAfterTimeout(operation, t.Exception)),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously
        );
        throw new TimeoutException($"{operation} timed out after {timeoutMs}ms");
    }

    private static string LateCompletionAfterTimeout(string operation, AggregateException exception) =>
        $"[Cloud] Late completion after timeout for '{operation}', result: {exception?.GetBaseException().Message}";
}
