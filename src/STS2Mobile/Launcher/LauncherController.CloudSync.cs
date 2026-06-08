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
            bool bypassConfirmation,
            Func<Task<string>> run
        )
        {
            ConfirmationMessage = confirmationMessage;
            Name = name;
            StartMessage = startMessage;
            CompleteMessage = completeMessage;
            BypassConfirmation = bypassConfirmation;
            Run = run;
        }

        internal string ConfirmationMessage { get; }
        internal bool BypassConfirmation { get; }
        private string Name { get; }
        private string StartMessage { get; }
        private string CompleteMessage { get; }
        private Func<Task<string>> Run { get; }

        internal static ManualCloudSyncRequest Push()
            => new(
                "Push local saves to cloud?\nThis will overwrite your cloud saves.",
                "Push",
                "Pushing local saves to cloud...",
                "Push complete.",
                false,
                LauncherCloudSaveState.ManualPushAllAsync
            );

        internal static ManualCloudSyncRequest Pull()
            => new(
                "Pull cloud saves to local?\nThis will overwrite your local saves.",
                "Pull",
                "Pulling cloud saves to local...",
                "Pull complete.",
                true,
                LauncherCloudSaveState.ManualPullAllAsync
            );

        internal void ShowStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            view.AppendLog(StartMessage);
        }

        internal void ShowComplete(LauncherView view, string result)
        {
            view.AppendLog(CompleteMessage);
            if (!string.IsNullOrWhiteSpace(result))
                view.AppendLog(result);
        }

        internal void ShowFailed(LauncherView view, Exception ex)
        {
            PatchHelper.Log($"[Cloud] {Name} sync failed: {ex.Message}");
            view.AppendLog($"{Name} failed: {ex.Message}");
        }

        internal void ShowFinished(LauncherView view)
            => view.SetPushPullDisabled(false);

        internal async Task<string> RunWithTimeoutAsync()
        {
            var operationTask = Task.Run(Run);
            await LauncherTimeout.RunOrThrowAsync(
                operationTask,
                CloudSyncTimeoutMs,
                $"{Name} timed out after {CloudSyncTimeoutMs}ms"
            );
            return await operationTask;
        }
    }

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Push());

    private void CloudPullPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Pull());

    private void RequestCloudSync(ManualCloudSyncRequest request)
    {
        _model.RefreshCloudSaveCredentials();

        if (request.BypassConfirmation)
        {
            _ = ExecuteCloudSyncAsync(request);
            return;
        }

        _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => _ = ExecuteCloudSyncAsync(request)
        );
    }

    private async Task ExecuteCloudSyncAsync(ManualCloudSyncRequest request)
    {
        RunOnMainThread(() => request.ShowStarted(_view));

        try
        {
            var result = await request.RunWithTimeoutAsync();
            RunOnMainThread(() => request.ShowComplete(_view, result));
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
