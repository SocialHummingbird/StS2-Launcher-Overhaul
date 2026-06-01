using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(
            "Push local saves to cloud?\nThis will overwrite your cloud saves.",
            LauncherCloudSaveState.ManualPushAllAsync,
            "Push",
            "Pushing local saves to cloud...",
            "Push complete."
        );

    private void CloudPullPressed()
        => RequestCloudSync(
            "Pull cloud saves to local?\nThis will overwrite your local saves.",
            LauncherCloudSaveState.ManualPullAllAsync,
            "Pull",
            "Pulling cloud saves to local...",
            "Pull complete."
        );

    private void RequestCloudSync(
        string confirmationMessage,
        Func<Task> run,
        string name,
        string startMessage,
        string completeMessage
    )
    {
        _view.ShowConfirmation(
            confirmationMessage,
            () => _ = ExecuteCloudSyncAsync(run, name, startMessage, completeMessage)
        );
    }

    private async Task ExecuteCloudSyncAsync(
        Func<Task> run,
        string name,
        string startMessage,
        string completeMessage
    )
    {
        SetCloudSyncBusy(startMessage);

        try
        {
            await RunCloudSyncWithTimeoutAsync(run, name);
            _runOnMainThread(() => _view.AppendLog(completeMessage));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] {name} sync failed: {ex.Message}");
            _runOnMainThread(() => _view.AppendLog($"{name} failed: {ex.Message}"));
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
