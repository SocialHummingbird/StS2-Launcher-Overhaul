namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed partial class StartupMode
    {
        private readonly PreviousStartupPhase _previousPhase;

        internal static StartupMode CreateFromMarkers()
            => new(
                PreviousStartupPhase.FromMarkers(),
                LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker()
            );

        private StartupMode(PreviousStartupPhase previousPhase, bool manualSafeLaunch)
        {
            _previousPhase = previousPhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        private bool ManualSafeLaunch { get; }

        private bool SafeLaunchRequested
            => ManualSafeLaunch || IsPreviousPhase(PhaseManualSafeLaunch);

        internal bool ShouldSkipShaderWarmup()
            => SafeLaunchRequested;

        internal string SettingsAndSavesStatus
            => "Loading settings and saves...";

        internal string ShaderWarmupSkipLog
            => SafeLaunchMessage(
                "Skipping shader warmup for manual safe launch",
                "Skipping shader warmup for safe launch"
            );

        internal string ShaderWarmupSkipStatus
            => SafeLaunchMessage(
                "Skipping shader warmup for safe launch...",
                "Skipping shader warmup for safe launch..."
            );

        private string SafeLaunchMessage(string manualSafeLaunch, string previousStall)
            => ManualSafeLaunch ? manualSafeLaunch : previousStall;

        private bool IsPreviousPhase(string phase)
            => _previousPhase.Matches(phase);
    }
}
