using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    private void OnProcessFrame()
    {
        AndroidBridgeDispatcher.Pump();
        DrainMainThreadActions();
        SyncViewportSize();
        _view?.UpdateKeyboardOffset();
    }

    private void EnqueueMainThreadAction(Action action) => _mainThreadActions.Enqueue(action);

    private void DrainMainThreadActions()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"UI update error: {ex.Message}");
            }
        }
    }
}
