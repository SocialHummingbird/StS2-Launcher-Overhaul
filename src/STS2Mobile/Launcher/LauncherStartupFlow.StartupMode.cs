using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed class StartupMode
    {
        private readonly string _previousIncompletePhase;

        public static StartupMode CreateFromMarkers()
            => new(
                LauncherLaunchMarkers.ReadStartupPhase(),
                LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker()
            );

        private StartupMode(string previousIncompletePhase, bool manualSafeLaunch)
        {
            _previousIncompletePhase = previousIncompletePhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        public bool ManualSafeLaunch { get; }

        public bool SafeLaunchRequested
            => ManualSafeLaunch || IsPreviousPhase(PhaseManualSafeLaunch);

        public bool ForceLocalSaves
            => SafeLaunchRequested
                || IsPreviousPhase(PhaseSettingsAndSaves)
                || IsPreviousPhase(PhaseGameStartup);

        public bool SkipShaderWarmup
            => SafeLaunchRequested || IsPreviousPhase(PhaseShaderWarmup);

        public string LocalSavesReasonLog
            => ManualSafeLaunch
                ? "Disabling cloud save injection for manual safe launch"
                : $"Disabling cloud save injection for this launch because previous launch stalled at {_previousIncompletePhase}";

        public string ShaderWarmupSkipLog
            => ManualSafeLaunch
                ? "Skipping shader warmup for manual safe launch"
                : "Skipping shader warmup because the previous launch stalled there";

        public string ShaderWarmupSkipStatus
            => ManualSafeLaunch
                ? "Skipping shader warmup for safe launch..."
                : "Skipping shader warmup after previous stall...";

        private bool IsPreviousPhase(string phase)
            => string.Equals(_previousIncompletePhase, phase, StringComparison.OrdinalIgnoreCase);
    }
}
