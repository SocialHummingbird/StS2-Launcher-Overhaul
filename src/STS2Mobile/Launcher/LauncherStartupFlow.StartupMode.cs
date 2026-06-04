using System;
using STS2Mobile.Patches;

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

        private StartupMode(string previousIncompletePhase, bool manualSafeLaunch)
        {
            _previousIncompletePhase = previousIncompletePhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        private bool ManualSafeLaunch { get; }

        private bool SafeLaunchRequested
            => ManualSafeLaunch || IsPreviousPhase(PhaseManualSafeLaunch);

        private bool ShouldForceLocalSaves()
            => SafeLaunchRequested
                || IsPreviousPhase(PhaseSettingsAndSaves)
                || IsPreviousPhase(PhaseGameStartup);

        internal bool ShouldSkipShaderWarmup()
            => SafeLaunchRequested || IsPreviousPhase(PhaseShaderWarmup);

        internal string SettingsAndSavesStatus
            => ShouldForceLocalSaves()
                ? "Loading settings and saves in local-only safe mode..."
                : "Loading settings and saves...";

        internal void ApplySaveMode()
        {
            LauncherPreferences.LoadAndApplyCloudSyncEnabled();
            if (!ShouldForceLocalSaves())
                return;

            LauncherCloudSaveState.DisableCloudSyncForLaunch();
            PatchHelper.Log(LocalSavesReasonLog);
        }

        private string LocalSavesReasonLog
            => SafeLaunchMessage(
                "Disabling cloud save injection for manual safe launch",
                $"Disabling cloud save injection for this launch because previous launch stalled at {_previousIncompletePhase}"
            );

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
            => string.Equals(_previousIncompletePhase, phase, StringComparison.OrdinalIgnoreCase);
    }
}
