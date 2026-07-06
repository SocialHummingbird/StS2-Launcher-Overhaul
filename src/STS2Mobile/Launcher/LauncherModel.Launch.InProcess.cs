using STS2Mobile.Patches;
using STS2Mobile.Steam;
using System;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private bool TrySignalInProcessLaunch(
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        bool safe,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
    {
        if (_launchTcs == null)
            return false;

        var selectedBranch = readiness.Branch;
        if (!string.Equals(_processGameBranch, selectedBranch, System.StringComparison.OrdinalIgnoreCase))
        {
            LauncherLaunchMarkers.RecordPhase(
                "launch requires restart",
                $"processBranch={_processGameBranch}; selectedBranch={selectedBranch}"
            );
            PatchHelper.Log(
                "[Launcher] Selected game branch changed from process-loaded "
                    + $"{SteamGameBranch.DisplayName(_processGameBranch)} to {SteamGameBranch.DisplayName(selectedBranch)}; "
                    + "restarting so Godot loads the selected version from disk."
            );
            return false;
        }

        if (!_launchTcs.TrySetResult(true))
        {
            LauncherLaunchMarkers.RecordPhase("in-process launch signal failed", $"branch={selectedBranch}");
            LauncherLaunchMarkers.WriteLaunchAttempt(
                LauncherLaunchAttemptPhases.InProcessSignalFailed,
                safe ? "safe" : "normal",
                launchSource,
                attemptId,
                readiness,
                modReadiness,
                preparedReadinessUsed: true,
                timingSnapshot(),
                "Launch signal could not be delivered to current game process; restart fallback will be requested."
            );
            return false;
        }

        LauncherLaunchMarkers.RecordPhase("in-process launch signalled", $"branch={selectedBranch}");
        LauncherLaunchMarkers.WriteLaunchAttempt(
            LauncherLaunchAttemptPhases.InProcessSignalled,
            safe ? "safe" : "normal",
            launchSource,
            attemptId,
            readiness,
            modReadiness,
            preparedReadinessUsed: true,
            timingSnapshot(),
            "Launch signal delivered to current game process from prepared readiness"
        );
        return true;
    }
}
