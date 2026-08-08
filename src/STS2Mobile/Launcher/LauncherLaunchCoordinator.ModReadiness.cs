using System;
namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private bool TryEvaluateModLaunchReadiness(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        LauncherLaunchReadiness readiness,
        out LauncherModLaunchReadiness modReadiness
    )
    {
        modReadiness = null;
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
            LauncherLaunchMarkers.RecordPhase(
                $"{plan.ModReadinessPhase}: loading selection",
                $"path={AppPaths.AppPrivateModSelectionPath}"
            );
            var selection = LauncherModSelectionState.Load();
            if (!LauncherModSelectionState.IsModdedModeFor(selection))
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{plan.ModReadinessPhase}: vanilla fast path",
                    "mod source scan skipped"
                );
                modReadiness = LauncherModLaunchReadiness.Vanilla(plan.ModReadinessPhase);
            }
            else
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{plan.ModReadinessPhase}: modded scan path",
                    "checking selected Workshop/manual mods"
                );
                modReadiness = LauncherModLaunchReadiness.Evaluate(plan.ModReadinessPhase, selection);
            }
        }
        catch (Exception ex)
        {
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
            return false;
        }
        attempt.StopModReadinessTiming();
        LauncherLaunchMarkers.RecordPhase(
            $"{plan.ModReadinessPhase}: passed",
            modReadiness.Summary
        );
        return true;
    }
}
