namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed class StartupMode
    {
        private readonly bool _safe;
        private StartupMode(bool safe) => _safe = safe;
        internal static StartupMode CreateFromMarkers()
            => new(LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker());
        internal bool ShouldSkipShaderWarmup() => _safe;
        internal string SettingsAndSavesStatus => "Loading settings and saves...";
        internal string ShaderWarmupSkipLog => "Skipping shader warmup for safe launch";
        internal string ShaderWarmupSkipStatus => "Skipping shader warmup for safe launch...";
    }
}
