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

        if (readiness.RuntimeSlot?.RequiresProcessRestartForPreparedRuntime == true)
        {
            var slot = readiness.RuntimeSlot;
            var detail = $"branch={selectedBranch}; activeAndroidAssemblySha256={slot.ActiveAndroidAssemblySha256}; preparedAndroidAssemblySha256={slot.PreparedAndroidAssemblySha256}";
            LauncherLaunchMarkers.RecordPhase("launch requires restart", detail);
            PatchHelper.Log(
                "[Launcher] Prepared Android game-code runtime does not match the assembly loaded by the current process; "
                    + $"restarting so Godot loads runtime pack {slot.RuntimePack?.PackId ?? "<unknown>"}. "
                    + $"active={slot.ActiveAndroidAssemblySha256} prepared={slot.PreparedAndroidAssemblySha256}"
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
