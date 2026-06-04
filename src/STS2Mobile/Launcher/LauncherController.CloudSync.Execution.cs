using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
}
