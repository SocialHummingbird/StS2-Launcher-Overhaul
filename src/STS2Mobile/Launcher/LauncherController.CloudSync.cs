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

        internal void ShowConfirmation(LauncherView view, Action accepted)
            => view.ShowConfirmation(ConfirmationMessage, accepted);

        internal Task RunWithTimeoutAsync()
            => RunCloudSyncWithTimeoutAsync(Run, Name);

        internal void MarkStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            view.AppendLog(StartMessage);
        }

        internal void MarkComplete(LauncherView view)
            => view.AppendLog(CompleteMessage);

        internal void MarkFailed(LauncherView view, Exception ex)
        {
            PatchHelper.Log($"[Cloud] {Name} sync failed: {ex.Message}");
            view.AppendLog($"{Name} failed: {ex.Message}");
        }

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
    }

    private sealed class CloudSyncExecution
    {
        private readonly CloudSyncRequest _request;
        private readonly LauncherView _view;
        private readonly Action<Action> _runOnMainThread;

        private CloudSyncExecution(
            CloudSyncRequest request,
            LauncherView view,
            Action<Action> runOnMainThread
        )
        {
            _request = request;
            _view = view;
            _runOnMainThread = runOnMainThread;
        }

        internal static CloudSyncExecution ForRequest(
            CloudSyncRequest request,
            LauncherView view,
            Action<Action> runOnMainThread
        )
            => new(request, view, runOnMainThread);

        internal async Task RunAsync()
        {
            OnMainThread(() => _request.MarkStarted(_view));

            try
            {
                await _request.RunWithTimeoutAsync();
                OnMainThread(() => _request.MarkComplete(_view));
            }
            catch (Exception ex)
            {
                OnMainThread(() => _request.MarkFailed(_view, ex));
            }
            finally
            {
                OnMainThread(() => _view.SetPushPullDisabled(false));
            }
        }

        private void OnMainThread(Action action)
            => _runOnMainThread(action);
    }

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(CloudSyncRequest.Push());

    private void CloudPullPressed()
        => RequestCloudSync(CloudSyncRequest.Pull());

    private void RequestCloudSync(CloudSyncRequest request)
        => request.ShowConfirmation(_view, () => _ = ExecuteCloudSyncAsync(request));

    private Task ExecuteCloudSyncAsync(CloudSyncRequest request)
        => CloudSyncExecution
            .ForRequest(request, _view, _runOnMainThread)
            .RunAsync();
}
