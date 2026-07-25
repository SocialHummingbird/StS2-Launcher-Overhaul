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
                ? "Game cloud sync enabled. Manual Push/Pull remains available from the launcher."
                : "Game cloud sync disabled. The game will use Android local saves; manual Push/Pull remains available."
        );
    }

    internal bool IsOperationActive
        => _operations.IsActive;

    internal Task CancelAndDrainAsync()
        => _operations.CancelAndDrainAsync();

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
