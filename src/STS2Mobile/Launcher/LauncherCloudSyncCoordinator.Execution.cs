using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal void CloudPullPressed()
        => RequestCloudSync(ManualCloudSyncRequest.Pull(
            _model.DataDir,
            LauncherPreferences.ReadGameBranch()
        ));

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
            () => _ = ExecuteCloudSyncAsync(request),
            request.ConfirmText,
            request.CancelText
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
