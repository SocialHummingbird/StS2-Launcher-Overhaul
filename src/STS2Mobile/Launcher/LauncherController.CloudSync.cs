using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int CloudSyncTimeoutMs = 180_000;

    private readonly struct ManualCloudSyncRequest
    {
        private ManualCloudSyncRequest(
            string confirmationMessage,
            string name,
            string startMessage,
            string completeMessage,
            Func<Task> run
        )
        {
            ConfirmationMessage = confirmationMessage;
            Name = name;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
            Run = run;
        }

        private string ConfirmationMessage { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }
        private Func<Task> Run { get; }

        internal static ManualCloudSyncRequest Push()
            => new(
                "Push local saves to cloud?\nThis will overwrite your cloud saves.",
                "Push",
                "Pushing local saves to cloud...",
                "Push complete.",
                LauncherCloudSaveState.ManualPushAllAsync
            );

        internal static ManualCloudSyncRequest Pull()
            => new(
                "Pull cloud saves to local?\nThis will overwrite your local saves.",
                "Pull",
                "Pulling cloud saves to local...",
                "Pull complete.",
                LauncherCloudSaveState.ManualPullAllAsync
            );

        internal void ShowStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            view.AppendLog(StartMessage);
        }

        internal void ShowComplete(LauncherView view)
            => view.AppendLog(CompleteMessage);

        internal void ShowFailed(LauncherView view, Exception ex)
        {
            PatchHelper.Log($"[Cloud] {Name} sync failed: {ex.Message}");
            view.AppendLog($"{Name} failed: {ex.Message}");
        }

        internal void ShowFinished(LauncherView view)
            => view.SetPushPullDisabled(false);

        internal async Task RunWithTimeoutAsync()
        {
            var operationTask = Run();
            await LauncherTimeout.RunOrThrowAsync(
                operationTask,
                CloudSyncTimeoutMs,
                $"{Name} timed out after {CloudSyncTimeoutMs}ms"
            );
        }
    }

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Push());

    private void CloudPullPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Pull());

    private void RequestCloudSync(ManualCloudSyncRequest request)
        => _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => _ = ExecuteCloudSyncAsync(request)
        );

    private async Task ExecuteCloudSyncAsync(ManualCloudSyncRequest request)
    {
        RunOnMainThread(() => request.ShowStarted(_view));

        try
        {
            await request.RunWithTimeoutAsync();
            RunOnMainThread(() => request.ShowComplete(_view));
        }
        catch (Exception ex)
        {
            RunOnMainThread(() => request.ShowFailed(_view, ex));
        }
        finally
        {
            RunOnMainThread(() => request.ShowFinished(_view));
        }
    }

    private void RunOnMainThread(Action action)
        => _runOnMainThread(action);
}
