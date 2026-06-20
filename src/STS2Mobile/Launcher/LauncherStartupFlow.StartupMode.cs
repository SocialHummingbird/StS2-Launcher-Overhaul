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

        private bool ShouldForceLocalSaves()
            => SafeLaunchRequested
                || IsPreviousPhase(PhaseSettingsAndSaves)
                || IsPreviousPhase(PhaseGameStartup);

        private StartupSaveModePlan SaveModePlan
            => StartupSaveModePlan.Create(
                ShouldForceLocalSaves(),
                LocalSavesReasonLog
            );

        internal bool ShouldSkipShaderWarmup()
            => SafeLaunchRequested || IsPreviousPhase(PhaseShaderWarmup);

        internal string SettingsAndSavesStatus
            => SaveModePlan.SettingsAndSavesStatus;

        internal void ApplySaveMode()
            => SaveModePlan.Apply();

        private string LocalSavesReasonLog
        {
            get
            {
                if (ManualSafeLaunch)
                    return "Disabling cloud access for manual safe launch; Android local saves remain available";

                return _previousPhase.DescribePreviousStall(
                    "Disabling cloud access for this launch because previous launch stalled at"
                );
            }
        }

        internal string ShaderWarmupSkipLog
            => SafeLaunchMessage(
                "Skipping shader warmup for manual safe launch",
                "Skipping shader warmup because the previous launch stalled there"
            );

        internal string ShaderWarmupSkipStatus
            => SafeLaunchMessage(
                "Skipping shader warmup for safe launch...",
                "Skipping shader warmup after previous stall..."
            );

        private string SafeLaunchMessage(string manualSafeLaunch, string previousStall)
            => ManualSafeLaunch ? manualSafeLaunch : previousStall;

        private bool IsPreviousPhase(string phase)
            => _previousPhase.Matches(phase);
    }
}
