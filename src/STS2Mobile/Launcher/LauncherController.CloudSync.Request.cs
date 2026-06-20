using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
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
            string completeMessage,
            bool bypassConfirmation,
            Func<Task<string>> run,
            Action? onComplete = null,
            Action<Exception>? onFailed = null
        )
        {
            ConfirmationMessage = confirmationMessage;
            ConfirmText = confirmText;
            CancelText = cancelText;
            Name = name;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
            BypassConfirmation = bypassConfirmation;
            Run = run;
            OnComplete = onComplete;
            OnFailed = onFailed;
        }

        internal string ConfirmationMessage { get; }
        internal string ConfirmText { get; }
        internal string CancelText { get; }
        internal bool BypassConfirmation { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }
        private Func<Task<string>> Run { get; }
        private Action? OnComplete { get; }
        private Action<Exception>? OnFailed { get; }
    }
}
