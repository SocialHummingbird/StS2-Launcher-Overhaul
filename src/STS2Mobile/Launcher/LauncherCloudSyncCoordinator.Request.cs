#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private const int CloudSyncTimeoutMs = 180_000;

    private readonly partial struct ManualCloudSyncRequest
    {
        private ManualCloudSyncRequest(
            string confirmationMessage,
            string confirmText,
            string cancelText,
            string name,
            string startMessage,
            bool bypassConfirmation,
            Func<CancellationToken, Task<ManualCloudSyncResult>> run,
            Func<bool>? prepareOperation = null,
            Action<string, string>? recordTerminalFailure = null,
            Action? onSuccessfulCompletion = null,
            Action<Exception>? onFailed = null,
            int timeoutMs = CloudSyncTimeoutMs,
            CloudOperationProgressTracker? operationProgress = null
        )
        {
            ConfirmationMessage = confirmationMessage;
            ConfirmText = confirmText;
            CancelText = cancelText;
            Name = name;
            StartMessage = startMessage;
            BypassConfirmation = bypassConfirmation;
            Run = run;
            PrepareOperation = prepareOperation;
            RecordTerminalFailureAction = recordTerminalFailure;
            OnSuccessfulCompletion = onSuccessfulCompletion;
            OnFailed = onFailed;
            TimeoutMs = timeoutMs;
            OperationProgress = operationProgress;
        }

        internal string ConfirmationMessage { get; }
        internal string ConfirmText { get; }
        internal string CancelText { get; }
        internal bool BypassConfirmation { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private Func<
            CancellationToken,
            Task<ManualCloudSyncResult>
        > Run { get; }
        private Func<bool>? PrepareOperation { get; }
        private Action<string, string>? RecordTerminalFailureAction { get; }
        private Action? OnSuccessfulCompletion { get; }
        private Action<Exception>? OnFailed { get; }
        private int TimeoutMs { get; }
        private CloudOperationProgressTracker? OperationProgress { get; }
    }
}
