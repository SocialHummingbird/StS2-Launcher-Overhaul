using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private int _modsPresentationRequest;
    private void OnDestinationSelected(LauncherDestination destination)
    {
        if (destination == LauncherDestination.Mods)
            RefreshModsPresentation();
    }

    private void RefreshModsPresentation()
    {
        var request = ++_modsPresentationRequest;
        _view.SetModsPresentationChecking();
        _ = RefreshModsPresentationAsync(request);
    }

    private async Task RefreshModsPresentationAsync(int request)
    {
        var presentation = await _model.ReadModsPresentationAsync().ConfigureAwait(false);
        _runOnMainThread(() =>
        {
            var host = _view.LaunchLifetimeHost;
            if (request == _modsPresentationRequest && GodotObject.IsInstanceValid(host) && host.IsInsideTree())
                _view.SetModsPresentation(presentation);
        });
    }
}
