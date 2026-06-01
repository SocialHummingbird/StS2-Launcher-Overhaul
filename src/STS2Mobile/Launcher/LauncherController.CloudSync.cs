using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void CloudSyncToggled(bool pressed)
        => LauncherPreferences.SaveCloudSyncEnabled(pressed);

    private void CloudPushPressed()
        => RequestCloudSync(CloudSyncRequest.Push);

    private void CloudPullPressed()
        => RequestCloudSync(CloudSyncRequest.Pull);

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
            await request.RunAsync();
            _runOnMainThread(() => _view.AppendLog(request.CompleteMessage));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] {request.OperationName} sync failed: {ex.Message}");
            _runOnMainThread(() => _view.AppendLog($"{request.OperationName} failed: {ex.Message}"));
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
