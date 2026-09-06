using System;
using System.Threading.Tasks;
namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private async Task<LauncherModLaunchReadiness> EvaluateModReadinessAsync(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        LauncherLaunchReadiness readiness,
        LauncherPreparationOverlay interactionLock
    )
    {
        LauncherModLaunchReadiness modReadiness = null;
        LauncherLaunchMarkers.RecordPhase(
            $"{plan.ModReadinessPhase}: entered",
            $"branch={attempt.Branch}"
        );
        LauncherLaunchMarkers.WriteLaunchAttempt(
            LauncherLaunchAttemptPhases.ModReadinessChecking,
            plan.Action,
            plan.Source,
            attempt.AttemptId,
            readiness,
            modReadiness: null,
            preparedReadinessUsed: true,
            attempt.ReadyTiming(),
            "Checking mod launch readiness before launch handoff."
        );
        attempt.StartModReadinessTiming();
        try
        {
            using var stage = new LauncherStartupStageScope(_view.LaunchLifetimeHost,
                LauncherHandoffStateOwner.Shared.GetOperation(attempt.AttemptId), TimeSpan.FromSeconds(60));
            var preparation = Task.Run(() =>
            {
            LauncherLaunchMarkers.RecordPhase(
                $"{plan.ModReadinessPhase}: loading selection",
                $"path={AppPaths.AppPrivateModSelectionPath}"
            );
            var selection = LauncherModSelectionState.Load();
            if (!LauncherModSelectionState.IsModdedModeFor(selection))
            {
                LauncherModLaunchResultStore.WriteVanilla(selection);
                LauncherLaunchMarkers.RecordPhase(
                    $"{plan.ModReadinessPhase}: vanilla fast path",
                    "mod source scan skipped"
                );
                return LauncherModLaunchReadiness.Vanilla(plan.ModReadinessPhase);
            }
            else
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{plan.ModReadinessPhase}: modded scan path",
                    "checking selected Workshop/manual mods"
                );
                return LauncherModLaunchReadiness.Evaluate(plan.ModReadinessPhase, selection);
            }
            });
            modReadiness = await LauncherPreparationLifetime.RunAsync(preparation, stage.WaitAsync,
                () => ShowPreparationDraining(interactionLock), ObservePreparationFailure);
            if (!IsCurrentAttempt(attempt)) return null;
        }
        catch (Exception ex)
        {
            if (!IsCurrentAttempt(attempt)) return null;
            attempt.StopModReadinessTiming();
            attempt.StopAttemptTiming();
            var problem = LaunchExceptionProblem("mod readiness check failed", ex);
            FinishFailedLaunchAttempt(
                plan,
                "launch mod readiness failed",
                problem,
                readiness,
                modReadiness: null,
                preparedReadinessUsed: true,
                attempt.FailedTiming(),
                LauncherLaunchAttemptPhases.ModReadinessFailed,
                problem,
                attempt.AttemptId,
                writePatchLog: true
            );
            return null;
        }
        attempt.StopModReadinessTiming();
        LauncherLaunchMarkers.RecordPhase(
            $"{plan.ModReadinessPhase}: passed",
            modReadiness.Summary
        );
        return modReadiness;
    }
}
