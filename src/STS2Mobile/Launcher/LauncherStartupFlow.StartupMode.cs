using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed class StartupMode
    {
        private readonly string _previousIncompletePhase;

        private static StartupMode CreateFromMarkers()
            => new(
                LauncherLaunchMarkers.ReadStartupPhase(),
                LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker()
            );

        private StartupMode(string previousIncompletePhase, bool manualSafeLaunch)
        {
            _previousIncompletePhase = previousIncompletePhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        private bool ManualSafeLaunch { get; }

        private bool SafeLaunchRequested
            => ManualSafeLaunch || IsPreviousPhase("manual safe launch");

        private bool ForceLocalSaves
            => SafeLaunchRequested
                || IsPreviousPhase("settings and saves")
                || IsPreviousPhase("game startup");

        private bool SkipShaderWarmup
            => SafeLaunchRequested || IsPreviousPhase("shader warmup");

        private string LocalSavesReasonLog
            => ManualSafeLaunch
                ? "Disabling cloud save injection for manual safe launch"
                : $"Disabling cloud save injection for this launch because previous launch stalled at {_previousIncompletePhase}";

        private string ShaderWarmupSkipLog
            => ManualSafeLaunch
                ? "Skipping shader warmup for manual safe launch"
                : "Skipping shader warmup because the previous launch stalled there";

        private string ShaderWarmupSkipStatus
            => ManualSafeLaunch
                ? "Skipping shader warmup for safe launch..."
                : "Skipping shader warmup after previous stall...";

        private bool IsPreviousPhase(string phase)
            => string.Equals(_previousIncompletePhase, phase, StringComparison.OrdinalIgnoreCase);
    }
}
