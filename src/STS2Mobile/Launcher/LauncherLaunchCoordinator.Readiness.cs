using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;
namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private async Task<LauncherLaunchReadiness> EvaluateLaunchReadinessAsync(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        LauncherPreparationOverlay interactionLock
    )
    {
        LauncherLaunchReadiness readiness = null;
        attempt.StartReadinessTiming();
        try
        {
            using var stage = new LauncherStartupStageScope(_view.LaunchLifetimeHost,
                LauncherHandoffStateOwner.Shared.GetOperation(attempt.AttemptId), TimeSpan.FromSeconds(60));
            await stage.ProcessFrameAsync();
            await stage.PostDrawAsync();
            var preparation = Task.Run(() =>
            {
                var prepared = EvaluateSelectedLaunchReadiness(attempt.Branch, plan);
                if (prepared.Ready) attempt.RestartRequest?.ValidateReadiness(prepared);
                return prepared;
            });
            readiness = await LauncherPreparationLifetime.RunAsync(preparation, stage.WaitAsync,
                () => ShowPreparationDraining(interactionLock), ObservePreparationFailure);
            if (!IsCurrentAttempt(attempt)) return null;
            if (SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()) != attempt.Branch)
                throw new InvalidOperationException("The selected game version changed while preparing launch. Press Play again.");
        }
        catch (Exception ex)
        {
            if (!IsCurrentAttempt(attempt)) return null;
            attempt.StopReadinessTiming();
            attempt.StopAttemptTiming();
            var problem = LaunchExceptionProblem("selected-version readiness check failed", ex);
            FinishFailedLaunchAttempt(
                plan,
                "launch readiness failed",
                problem,
                attempt.PendingReadiness,
                modReadiness: null,
                preparedReadinessUsed: false,
                attempt.FailedTiming(),
                LauncherLaunchAttemptPhases.ReadinessFailed,
                problem,
                attempt.AttemptId,
                writePatchLog: true
            );
            return null;
        }
        attempt.StopReadinessTiming();
        if (!readiness.Ready)
        {
            attempt.StopAttemptTiming();
            var problem = readiness.ReadinessProblem;
            FinishFailedLaunchAttempt(
                plan,
                plan.BlockedPhase,
                $"branch={attempt.Branch}; problem={problem}",
                readiness,
                modReadiness: null,
                preparedReadinessUsed: true,
                attempt.BlockedTiming(),
                LauncherLaunchAttemptPhases.Blocked,
                problem,
                attempt.AttemptId,
                writePatchLog: false
            );
            return null;
        }

        LauncherLaunchMarkers.RecordPhase(plan.ReadinessPassedPhase, $"branch={attempt.Branch}");
        return readiness;
    }

    private bool IsCurrentAttempt(LaunchAttemptContext attempt)
        => ReferenceEquals(_activeLaunchAttempt, attempt)
            && LauncherHandoffStateOwner.Shared.Capture().AttemptId == attempt.AttemptId
            && LauncherHandoffStateOwner.Shared.Capture().State == LauncherHandoffState.HandoffPending;

    private LauncherLaunchReadiness EvaluateSelectedLaunchReadiness(
        string branch,
        LauncherStartGamePlan plan
    )
        => LauncherLaunchReadiness.Evaluate(
            _model.DataDir,
            branch,
            plan.ReadinessPhase
        );
}
