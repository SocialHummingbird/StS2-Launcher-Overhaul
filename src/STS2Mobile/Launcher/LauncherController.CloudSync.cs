using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct CloudSyncRequest
    {
        private CloudSyncRequest(
            string confirmationMessage,
            Func<Task> run,
            string name,
            string startMessage,
            string completeMessage
        )
        {
            ConfirmationMessage = confirmationMessage;
            Run = run;
            Name = name;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
        }

        private string ConfirmationMessage { get; }
        private Func<Task> Run { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }

        internal static CloudSyncRequest Push()
            => new(
                "Push local saves to cloud?\nThis will overwrite your cloud saves.",
                LauncherCloudSaveState.ManualPushAllAsync,
                "Push",
                "Pushing local saves to cloud...",
                "Push complete."
            );

        internal static CloudSyncRequest Pull()
            => new(
                "Pull cloud saves to local?\nThis will overwrite your local saves.",
                LauncherCloudSaveState.ManualPullAllAsync,
                "Pull",
                "Pulling cloud saves to local...",
                "Pull complete."
            );

        internal void Confirm(LauncherView view, Action onConfirmed)
            => view.ShowConfirmation(ConfirmationMessage, onConfirmed);

        internal async Task ExecuteAsync(
            LauncherView view,
            Action<Action> runOnMainThread
        )
        {
            var request = this;
            runOnMainThread(() => request.MarkStarted(view));

            try
            {
                await request.RunWithTimeoutAsync();
                runOnMainThread(() => request.MarkCompleted(view));
            }
            catch (Exception ex)
            {
                runOnMainThread(() => request.MarkFailed(view, ex));
            }
            finally
            {
                runOnMainThread(() => view.SetPushPullDisabled(false));
            }
        }

        private Task RunWithTimeoutAsync()
            => RunCloudSyncWithTimeoutAsync(Run, Name);

        private void MarkStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            view.AppendLog(StartMessage);
        }

        private void MarkCompleted(LauncherView view)
            => view.AppendLog(CompleteMessage);

        private void MarkFailed(LauncherView view, Exception ex)
        {
            PatchHelper.Log($"[Cloud] {Name} sync failed: {ex.Message}");
            view.AppendLog($"{Name} failed: {ex.Message}");
        }
    }

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(CloudSyncRequest.Push());

    private void CloudPullPressed()
        => RequestCloudSync(CloudSyncRequest.Pull());

    private void RequestCloudSync(CloudSyncRequest request)
        => request.Confirm(
            _view,
            () => _ = request.ExecuteAsync(_view, _runOnMainThread)
        );
}
