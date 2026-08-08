using System;
namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private bool TryEvaluateSelectedLaunchReadiness(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        out LauncherLaunchReadiness readiness
    )
    {
        readiness = null;
        attempt.StartReadinessTiming();
        try
        {
            readiness = EvaluateSelectedLaunchReadiness(attempt.Branch, plan);
        }
        catch (Exception ex)
        {
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
            return false;
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
            return false;
        }

        LauncherLaunchMarkers.RecordPhase(plan.ReadinessPassedPhase, $"branch={attempt.Branch}");
        return true;
    }

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
