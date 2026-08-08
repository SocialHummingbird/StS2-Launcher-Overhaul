using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator : IDisposable
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action<Action> _runOnMainThread;
    private readonly CloudOperationSessionManager _operations = new();
    private volatile bool _disposed;

    internal LauncherCloudSyncCoordinator(
        LauncherModel model,
        LauncherView view,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _runOnMainThread = runOnMainThread;
    }

    internal void CloudSyncToggled(bool pressed)
    {
        LauncherPreferences.SaveCloudSyncEnabled(pressed);
        _view.SetStatus(
            pressed
                ? "Automatic Steam save sync enabled. The launcher will reconcile saves before and after the game."
                : "Automatic Steam save sync disabled. The game will use Android local saves; any existing pending sync must still recover first."
        );
    }

    internal bool IsOperationActive
        => _operations.IsActive;

    internal Task CancelAndDrainAsync()
        => _operations.CancelAndDrainAsync();

    // Recovery is local-only, but it shares this one operation owner so a
    // restore can never overlap an in-flight Steam transfer.
    internal bool TryRunExclusiveSaveOperation(
        Func<System.Threading.CancellationToken, Task> run,
        out Task execution
    )
        => _operations.TryRun(run, out execution);

    internal void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _operations.Dispose();
    }

    void IDisposable.Dispose()
        => Dispose();
}
