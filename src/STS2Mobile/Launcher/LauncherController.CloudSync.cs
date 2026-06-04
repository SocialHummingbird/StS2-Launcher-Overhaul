using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
