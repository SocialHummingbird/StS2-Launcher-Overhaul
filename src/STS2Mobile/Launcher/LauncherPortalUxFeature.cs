namespace STS2Mobile.Launcher;

internal readonly struct LauncherPortalUxFeature
{
    internal LauncherPortalUxFeature(string diagnosticLabel, bool supported)
    {
        DiagnosticLabel = diagnosticLabel;
        Supported = supported;
    }

    internal string DiagnosticLabel { get; }

    internal bool Supported { get; }
}
