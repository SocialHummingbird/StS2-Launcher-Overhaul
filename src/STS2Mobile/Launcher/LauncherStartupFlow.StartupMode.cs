using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed class StartupMode
    {
        private readonly string _previousIncompletePhase;

        internal static StartupMode CreateFromMarkers()
            => new(
                LauncherLaunchMarkers.ReadStartupPhase(),
                LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker()
            );

        internal StartupMode(string previousIncompletePhase, bool manualSafeLaunch)
        {
            _previousIncompletePhase = previousIncompletePhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        private bool ManualSafeLaunch { get; }

        private bool SafeLaunchRequested
            => ManualSafeLaunch || IsPreviousPhase("manual safe launch");

        internal bool ForceLocalSaves
            => SafeLaunchRequested
                || IsPreviousPhase("settings and saves")
                || IsPreviousPhase("game startup");

        internal bool SkipShaderWarmup
            => SafeLaunchRequested || IsPreviousPhase("shader warmup");

        internal string LocalSavesReasonLog
            => ManualSafeLaunch
                ? "Disabling cloud save injection for manual safe launch"
                : $"Disabling cloud save injection for this launch because previous launch stalled at {_previousIncompletePhase}";

        internal string ShaderWarmupSkipLog
            => ManualSafeLaunch
                ? "Skipping shader warmup for manual safe launch"
                : "Skipping shader warmup because the previous launch stalled there";

        internal string ShaderWarmupSkipStatus
            => ManualSafeLaunch
                ? "Skipping shader warmup for safe launch..."
                : "Skipping shader warmup after previous stall...";

        private bool IsPreviousPhase(string phase)
            => string.Equals(_previousIncompletePhase, phase, StringComparison.OrdinalIgnoreCase);
    }
}
