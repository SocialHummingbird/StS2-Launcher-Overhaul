using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchReadiness
{
    internal static LauncherLaunchReadiness Evaluate(
        string dataDir,
        string branch,
        string phase
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        if (LauncherLaunchReadinessCache.TryGet(dataDir, branch, phase, out var cached))
            return cached;

        var readiness = EvaluateFresh(dataDir, branch, phase);
        LauncherLaunchReadinessCache.Store(dataDir, readiness);
        return readiness;
    }

    private static LauncherLaunchReadiness EvaluateFresh(
        string dataDir,
        string branch,
        string phase
    )
    {
        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: checking selected game files",
            $"branch={branch}"
        );

        if (!LauncherGameFiles.DownloadedForValidation(dataDir, branch, out var downloadProblem))
        {
            var problem = downloadProblem
                ?? "Selected game version is not ready to launch.";
            LauncherLaunchMarkers.RecordPhase($"{phase}: selected game files blocked", problem);
            return new LauncherLaunchReadiness(
                dataDir,
                branch,
                ready: false,
                problem,
                runtimeSlot: null,
                phase,
                LauncherLaunchReadinessCacheStatus.Fresh
            );
        }

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: validating patch compatibility",
            $"branch={branch}"
        );
        var slot = PatchCompatibilityValidator.ValidateSelectedVersionSlot(dataDir, branch);

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: runtime pairing inspected",
            $"branch={branch}; slot={slot.RuntimeSlotId}"
        );
        var readinessProblem = slot.Playable ? string.Empty : slot.ReadinessProblem();
        LauncherRuntimeSlotEvidence.Write(dataDir, slot, slot.Playable, readinessProblem);

        LauncherLaunchMarkers.RecordPhase(
            slot.Playable ? $"{phase}: readiness passed" : $"{phase}: readiness blocked",
            $"branch={branch}; slot={slot.RuntimeSlotId}; runtime={slot.RuntimePairingStatus}; patch={slot.PatchCompatibility?.Status ?? "<none>"}; problem={readinessProblem}"
        );

        return new LauncherLaunchReadiness(
            dataDir,
            branch,
            slot.Playable,
            readinessProblem,
            slot,
            phase,
            LauncherLaunchReadinessCacheStatus.Fresh
        );
    }
}
