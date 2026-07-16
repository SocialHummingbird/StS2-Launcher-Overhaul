namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void RendererModeChanged(string mode)
    {
        var normalized = LauncherRendererMode.Normalize(mode);
        LauncherPreferences.SaveRendererMode(normalized);
        LauncherLaunchMarkers.RecordPhase(
            "renderer preference changed",
            $"renderer={normalized}"
        );
        _view.SetRendererMode(normalized);
    }
}
