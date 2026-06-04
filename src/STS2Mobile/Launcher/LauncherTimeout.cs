using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class LauncherTimeout
{
    internal static async Task<bool> CompletesWithinAsync(Task task, int timeoutMs)
        => await Task.WhenAny(task, Task.Delay(timeoutMs)) == task;

    internal static async Task RunOrThrowAsync(
        Task task,
        int timeoutMs,
        string timeoutMessage
    )
    {
        if (!await CompletesWithinAsync(task, timeoutMs))
            throw new TimeoutException(timeoutMessage);

        await task;
    }

    internal static async Task<bool> RecoverIfTimedOutAsync(
        Task task,
        int timeoutMs,
        Func<Task> recoverAsync
    )
    {
        if (await CompletesWithinAsync(task, timeoutMs))
            return false;

        await recoverAsync();
        return true;
    }
}
