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

        if (!LauncherGameFiles.TryReadReadyIdentity(
                dataDir,
                branch,
                out var gameIdentity,
                out var downloadProblem
            ))
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

        return EvaluateReadyIdentity(dataDir, gameIdentity, phase);
    }

    internal static LauncherLaunchReadiness EvaluateCompletedInstall(
        string dataDir,
        GameIdentity gameIdentity,
        string phase
    )
    {
        if (gameIdentity == null)
            throw new System.ArgumentNullException(nameof(gameIdentity));

        if (!BranchInstallStateStore.Current.TryReadReady(
                dataDir,
                gameIdentity.Branch,
                gameIdentity,
                out _,
                out var stateProblem
            ))
        {
            return new LauncherLaunchReadiness(
                dataDir,
                gameIdentity.Branch,
                ready: false,
                stateProblem,
                runtimeSlot: null,
                phase,
                LauncherLaunchReadinessCacheStatus.Fresh
            );
        }

        var readiness = EvaluateReadyIdentity(dataDir, gameIdentity, phase);
        LauncherLaunchReadinessCache.Store(dataDir, readiness);
        return readiness;
    }

    private static LauncherLaunchReadiness EvaluateReadyIdentity(
        string dataDir,
        GameIdentity gameIdentity,
        string phase
    )
    {
        var branch = gameIdentity.Branch;

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: validating patch compatibility",
            $"branch={branch}; gameIdentity={gameIdentity.Id}"
        );
        var inspectedSlot = GameRuntimeSlot.Inspect(dataDir, gameIdentity);
        var validation = PatchCompatibilityValidator.ValidateSelectedVersionSlot(
            dataDir,
            gameIdentity,
            inspectedSlot
        );
        var slot = validation.RuntimeSlot;

        if (!string.IsNullOrWhiteSpace(validation.Problem))
        {
            LauncherRuntimeSlotEvidence.Revoke(dataDir);
            LauncherLaunchMarkers.RecordPhase(
                $"{phase}: runtime-pack candidate blocked",
                $"branch={branch}; gameIdentity={gameIdentity.Id}; problem={validation.Problem}"
            );
            return new LauncherLaunchReadiness(
                dataDir,
                branch,
                ready: false,
                validation.Problem,
                slot,
                phase,
                LauncherLaunchReadinessCacheStatus.Fresh
            );
        }

        var launchPreparation = RuntimePackLaunchLifecycle.Complete(
            dataDir,
            gameIdentity,
            validation.Candidate
        );
        slot = launchPreparation.RuntimeSlot ?? GameRuntimeSlot.Inspect(dataDir, gameIdentity);

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: runtime pairing inspected",
            $"branch={branch}; gameIdentity={slot.GameIdentityId}"
        );
        var ready = launchPreparation.Succeeded && slot.Playable;
        var readinessProblem = ready
            ? string.Empty
            : !string.IsNullOrWhiteSpace(launchPreparation.Problem)
                ? launchPreparation.Problem
                : slot.ReadinessProblem();

        LauncherLaunchMarkers.RecordPhase(
            ready ? $"{phase}: readiness passed" : $"{phase}: readiness blocked",
            $"branch={branch}; gameIdentity={slot.GameIdentityId}; runtime={slot.RuntimePairingStatus}; patch={slot.PatchCompatibility?.Status ?? "<none>"}; problem={readinessProblem}"
        );

        return new LauncherLaunchReadiness(
            dataDir,
            branch,
            ready,
            readinessProblem,
            slot,
            phase,
            LauncherLaunchReadinessCacheStatus.Fresh
        );
    }
}
