using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int CloudSyncTimeoutMs = 180_000;

    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(
            "Push local saves to cloud?\nThis will overwrite your cloud saves.",
            "Push",
            "Pushing local saves to cloud...",
            "Push complete.",
            LauncherCloudSaveState.ManualPushAllAsync
        );

    private void CloudPullPressed()
        => RequestCloudSync(
            "Pull cloud saves to local?\nThis will overwrite your local saves.",
            "Pull",
            "Pulling cloud saves to local...",
            "Pull complete.",
            LauncherCloudSaveState.ManualPullAllAsync
        );

    private void RequestCloudSync(
        string confirmationMessage,
        string name,
        string startMessage,
        string completeMessage,
        Func<Task> run
    )
        => _view.ShowConfirmation(
            confirmationMessage,
            () => _ = ExecuteCloudSyncAsync(
                name,
                startMessage,
                completeMessage,
                run
            )
        );

    private async Task ExecuteCloudSyncAsync(
        string name,
        string startMessage,
        string completeMessage,
        Func<Task> run
    )
    {
        RunOnMainThread(() =>
        {
            _view.SetPushPullDisabled(true);
            _view.AppendLog(startMessage);
        });

        try
        {
            await RunCloudSyncWithTimeoutAsync(name, run);
            RunOnMainThread(() => _view.AppendLog(completeMessage));
        }
        catch (Exception ex)
        {
            RunOnMainThread(() =>
            {
                PatchHelper.Log($"[Cloud] {name} sync failed: {ex.Message}");
                _view.AppendLog($"{name} failed: {ex.Message}");
            });
        }
        finally
        {
            RunOnMainThread(() => _view.SetPushPullDisabled(false));
        }
    }

    private static async Task RunCloudSyncWithTimeoutAsync(
        string name,
        Func<Task> run
    )
    {
        var operationTask = run();
        await LauncherTimeout.RunOrThrowAsync(
            operationTask,
            CloudSyncTimeoutMs,
            $"{name} timed out after {CloudSyncTimeoutMs}ms"
        );
    }

    private void RunOnMainThread(Action action)
        => _runOnMainThread(action);
}
