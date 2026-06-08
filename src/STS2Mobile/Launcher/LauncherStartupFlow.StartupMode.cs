using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private sealed class StartupMode
    {
        private readonly PreviousStartupPhase _previousPhase;

        private readonly struct PreviousStartupPhase
        {
            private PreviousStartupPhase(string phase)
            {
                Phase = phase;
            }

            private string Phase { get; }

            private bool Is(string phase)
                => string.Equals(Phase, phase, StringComparison.OrdinalIgnoreCase);

            private string PreviousStallMessage(string message)
                => $"{message} {Phase}";

            internal static PreviousStartupPhase FromMarkers()
                => new(LauncherLaunchMarkers.ReadStartupPhase());

            internal bool Matches(string phase)
                => Is(phase);

            internal string DescribePreviousStall(string message)
                => PreviousStallMessage(message);
        }

        private readonly struct StartupSaveModePlan
        {
            private StartupSaveModePlan(bool forceLocalSaves, string reasonLog)
            {
                ForceLocalSaves = forceLocalSaves;
                ReasonLog = reasonLog;
            }

            private bool ForceLocalSaves { get; }
            private string ReasonLog { get; }

            internal string SettingsAndSavesStatus
                => ForceLocalSaves
                    ? "Loading settings and saves in local-only safe mode..."
                    : "Loading settings and saves...";

            internal static StartupSaveModePlan Create(
                bool forceLocalSaves,
                string reasonLog
            )
                => new(forceLocalSaves, reasonLog);

            internal void Apply()
            {
                LauncherPreferences.LoadAndApplyCloudSyncEnabled();
                if (!ForceLocalSaves)
                    return;

                LauncherCloudSaveState.DisableCloudSyncForLaunch();
                PatchHelper.Log(ReasonLog);
            }
        }

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
