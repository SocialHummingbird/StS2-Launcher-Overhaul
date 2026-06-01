using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct CloudSyncRequest
    {
        private const int OperationTimeoutMs = 180_000;

        private enum CloudSyncOperation
        {
            Push,
            Pull,
        }

        private CloudSyncRequest(
            string confirmationMessage,
            CloudSyncOperation operation,
            string operationName,
            string startMessage,
            string completeMessage
        )
        {
            ConfirmationMessage = confirmationMessage;
            Operation = operation;
            OperationName = operationName;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
        }

        private string ConfirmationMessage { get; }
        private CloudSyncOperation Operation { get; }
        private string OperationName { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }

        private static CloudSyncRequest Push => new(
            "Push local saves to cloud?\nThis will overwrite your cloud saves.",
            CloudSyncOperation.Push,
            "Push",
            "Pushing local saves to cloud...",
            "Push complete."
        );

        private static CloudSyncRequest Pull => new(
            "Pull cloud saves to local?\nThis will overwrite your local saves.",
            CloudSyncOperation.Pull,
            "Pull",
            "Pulling cloud saves to local...",
            "Pull complete."
        );

        private async Task RunAsync()
        {
            var operationTask = RunOperationAsync();
            var timeout = Task.Delay(OperationTimeoutMs);
            if (await Task.WhenAny(operationTask, timeout) != operationTask)
                throw new System.TimeoutException(
                    $"{OperationName} timed out after {OperationTimeoutMs}ms"
                );

            await operationTask;
        }

        private Task RunOperationAsync()
            => Operation switch
            {
                CloudSyncOperation.Push => LauncherCloudSaveState.ManualPushAllAsync(),
                CloudSyncOperation.Pull => LauncherCloudSaveState.ManualPullAllAsync(),
                _ => throw new System.InvalidOperationException(
                    $"Unknown cloud sync operation: {Operation}"
                ),
            };
    }
}
