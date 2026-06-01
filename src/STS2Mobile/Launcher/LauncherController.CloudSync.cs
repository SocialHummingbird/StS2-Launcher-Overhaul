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

        internal string ConfirmationMessage { get; }
        internal Func<Task> Run { get; }
        internal string Name { get; }
        internal string StartMessage { get; }
        internal string CompleteMessage { get; }

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

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(CloudSyncRequest.Push());

    private void CloudPullPressed()
        => RequestCloudSync(CloudSyncRequest.Pull());

    private void RequestCloudSync(CloudSyncRequest request)
    {
        _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => _ = ExecuteCloudSyncAsync(request)
        );
    }

    private async Task ExecuteCloudSyncAsync(CloudSyncRequest request)
    {
        SetCloudSyncBusy(request.StartMessage);

        try
        {
            await RunCloudSyncWithTimeoutAsync(request.Run, request.Name);
            _runOnMainThread(() => _view.AppendLog(request.CompleteMessage));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] {request.Name} sync failed: {ex.Message}");
            _runOnMainThread(() =>
                _view.AppendLog($"{request.Name} failed: {ex.Message}")
            );
        }
        finally
        {
            _runOnMainThread(() => _view.Actions.SetPushPullDisabled(false));
        }
    }

    private void SetCloudSyncBusy(string startMessage)
    {
        _runOnMainThread(() =>
        {
            _view.Actions.SetPushPullDisabled(true);
            _view.AppendLog(startMessage);
        });
    }
}
