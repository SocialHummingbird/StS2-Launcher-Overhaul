using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int CloudSyncTimeoutMs = 180_000;

    private readonly struct TimedCloudSyncOperation
    {
        private readonly Func<Task> _run;

        private TimedCloudSyncOperation(string name, Func<Task> run)
        {
            Name = name;
            _run = run;
        }

        private string Name { get; }
        private string TimeoutMessage
            => $"{Name} timed out after {CloudSyncTimeoutMs}ms";

        internal static TimedCloudSyncOperation For(string name, Func<Task> run)
            => new(name, run);

        internal async Task RunAsync()
        {
            var operationTask = _run();
            var timeout = Task.Delay(CloudSyncTimeoutMs);
            if (await Task.WhenAny(operationTask, timeout) != operationTask)
                throw new TimeoutException(TimeoutMessage);

            await operationTask;
        }
    }
}
