using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int CloudSyncTimeoutMs = 180_000;

    private static async Task RunCloudSyncWithTimeoutAsync(
        Func<Task> runOperation,
        string operationName
    )
    {
        var operationTask = runOperation();
        var timeout = Task.Delay(CloudSyncTimeoutMs);
        if (await Task.WhenAny(operationTask, timeout) != operationTask)
            throw new TimeoutException(
                $"{operationName} timed out after {CloudSyncTimeoutMs}ms"
            );

        await operationTask;
    }
}
