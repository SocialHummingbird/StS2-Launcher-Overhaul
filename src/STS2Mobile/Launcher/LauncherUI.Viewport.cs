using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    private void SyncViewportSize()
    {
        var viewportSize = GetViewportSize();
        if (_lastViewportSize.DistanceSquaredTo(viewportSize) <= 1f)
            return;

        _lastViewportSize = viewportSize;
        Size = viewportSize;
        _view?.UpdateViewportSize(viewportSize);
    }

    private Vector2 GetViewportSize()
        => GetViewport()?.GetVisibleRect().Size ?? DefaultViewportSize;
}
