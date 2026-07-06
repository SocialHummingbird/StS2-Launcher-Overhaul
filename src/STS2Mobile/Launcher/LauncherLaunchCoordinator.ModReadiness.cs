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
        attempt.StartModReadinessTiming();
        try
        {
            modReadiness = LauncherModLaunchReadiness.Evaluate(plan.ModReadinessPhase);
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
