using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private void CompleteLaunchHandoff(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness
    )
    {
        LauncherLaunchMarkers.RecordPhase(
            "launch handoff: entered",
            $"action={plan.Action}; source={plan.Source}; branch={attempt.Branch}"
        );
        _view.SetStatus(plan.StartingStatus, LauncherStatusSeverity.Working);
        if (!string.IsNullOrWhiteSpace(plan.PreLaunchLog))
            _view.AppendLog(plan.PreLaunchLog);

        var timing = attempt.ReadyTiming();
        LauncherLaunchMarkers.RecordPhase(
            "launch handoff: writing ready marker",
            modReadiness?.Summary
        );
        LauncherLaunchMarkers.WriteLaunchAttempt(
            LauncherLaunchAttemptPhases.Ready,
            plan.Action,
            plan.Source,
            attempt.AttemptId,
            readiness,
            modReadiness,
            preparedReadinessUsed: true,
            timing,
            plan.ReadyDetail
        );
        try
        {
            LauncherLaunchMarkers.RecordPhase(
                "launch handoff: calling model launch",
                $"action={plan.Action}; source={plan.Source}; branch={attempt.Branch}"
            );
            var handoff = plan.Launch(_model, readiness, modReadiness, attempt.AttemptId, attempt.ReadyTiming);
            LauncherLaunchMarkers.RecordPhase(
                "launch handoff: model returned",
                $"requested={handoff.Requested}; phase={handoff.RequestedPhase}"
            );
            if (handoff.Requested)
            {
                if (!LauncherLaunchAttemptPhases.IsSuccessfulHandoffPhase(handoff.RequestedPhase))
                {
                    var missingPhaseProblem = $"Launch handoff was requested without an accepted evidence phase: {handoff.RequestedPhase}";
                    attempt.StopAttemptTiming();
                    timing = attempt.ReadyTiming();
                    FinishFailedLaunchAttempt(
                        plan,
                        "launch handoff missing accepted requested phase",
                        missingPhaseProblem,
                        readiness,
                        modReadiness,
                        preparedReadinessUsed: true,
                        timing,
                        LauncherLaunchAttemptPhases.LaunchHandoffNotRequested,
                        missingPhaseProblem,
                        attempt.AttemptId,
                        writePatchLog: true
                    );
                    return;
                }

                attempt.StopAttemptTiming();
                return;
            }

            var markerPhase = string.IsNullOrWhiteSpace(handoff.FailurePhase)
                ? LauncherLaunchAttemptPhases.LaunchHandoffNotRequested
                : handoff.FailurePhase;
            var problem = string.IsNullOrWhiteSpace(handoff.FailureProblem)
                ? "Launch handoff was not requested after prepared readiness."
                : handoff.FailureProblem;
            attempt.StopAttemptTiming();
            timing = attempt.ReadyTiming();
            FinishFailedLaunchAttempt(
                plan,
                markerPhase,
                problem,
                readiness,
                modReadiness,
                preparedReadinessUsed: true,
                timing,
                markerPhase,
                problem,
                attempt.AttemptId,
                handoff.WritePatchLog
            );
        }
        catch (Exception ex)
        {
            var problem = LaunchExceptionProblem("launch handoff failed", ex);
            attempt.StopAttemptTiming();
            timing = attempt.ReadyTiming();
            FinishFailedLaunchAttempt(
                plan,
                "launch handoff failed",
                problem,
                readiness,
                modReadiness,
                preparedReadinessUsed: true,
                timing,
                LauncherLaunchAttemptPhases.LaunchHandoffFailed,
                problem,
                attempt.AttemptId,
                writePatchLog: true
            );
        }
    }
}
