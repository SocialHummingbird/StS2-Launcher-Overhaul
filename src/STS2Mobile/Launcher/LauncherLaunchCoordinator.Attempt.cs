using System;
using System.Diagnostics;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private bool TryBeginLaunchAttempt(LauncherStartGamePlan plan, out LaunchAttemptContext attempt)
    {
        attempt = null;
        if (_launchInProgress)
        {
            var ignoredBranch = ActiveLaunchAttemptBranch();
            LauncherLaunchMarkers.RecordPhase(
                "launch request ignored",
                $"action={plan.Action}; source={plan.Source}; branch={ignoredBranch}; reason=launch already in progress"
            );
            _view.SetStatus(
                "Start Game is already running. Waiting for launch handoff...",
                LauncherStatusSeverity.Working
            );
            return false;
        }

        SetLaunchInProgress(true);
        string attemptId = null;
        var attemptTimer = Stopwatch.StartNew();
        try
        {
            var restoringPendingAttempt = string.Equals(
                plan.Source,
                LauncherLaunchSource.AutoLaunch,
                StringComparison.Ordinal
            );
            var restartRequest = restoringPendingAttempt ? LauncherRestartRequest.ConsumeRestored() : null;
            attemptId = restartRequest?.AttemptId ?? LaunchAttemptContext.CreateAttemptId();
            var branch = SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
            if (restartRequest != null && (restartRequest.Branch != branch || restartRequest.Safe != plan.IsSafe))
                throw new InvalidOperationException("The restored launch request no longer matches the selected version or launch mode. Press Play again.");
            LauncherLaunchMarkers.RecordPhase(
                plan.ButtonPressedPhase,
                $"branch={branch}"
            );
            var pendingReadiness = LauncherLaunchReadiness.Pending(
                _model.DataDir,
                branch,
                plan.ReadinessPhase,
                "Start Game request accepted; selected-version readiness has not completed."
            );
            attempt = new LaunchAttemptContext(attemptId, attemptTimer, branch, pendingReadiness);
            attempt.RestartRequest = restartRequest;
            LauncherLaunchMarkers.WriteLaunchAttempt(
                LauncherLaunchAttemptPhases.Checking,
                plan.Action,
                plan.Source,
                attempt.AttemptId,
                pendingReadiness,
                modReadiness: null,
                preparedReadinessUsed: false,
                LauncherLaunchAttemptTiming.NotMeasured(),
                "Checking selected-version readiness before launch handoff."
            );
            var ownerAccepted = restoringPendingAttempt
                ? LauncherHandoffStateOwner.Shared.RestorePending(attempt.AttemptId)
                : LauncherHandoffStateOwner.Shared.Begin(attempt.AttemptId);
            if (!ownerAccepted)
                throw new InvalidOperationException(
                    "The authoritative handoff owner rejected the new launch attempt."
                );
            _activeLaunchAttempt = attempt;
            return true;
        }
        catch (Exception ex)
        {
            attemptTimer.Stop();
            var problem = LaunchExceptionProblem("launch setup failed", ex);
            var pendingReadiness = LauncherLaunchReadiness.Pending(
                _model.DataDir,
                SteamGameBranch.Public,
                plan.ReadinessPhase,
                problem
            );
            FinishFailedLaunchAttempt(
                plan,
                "launch setup failed",
                problem,
                pendingReadiness,
                modReadiness: null,
                preparedReadinessUsed: false,
                LauncherLaunchAttemptTiming.Failed(attemptTimer, readinessTimer: null, modReadinessTimer: null),
                LauncherLaunchAttemptPhases.SetupFailed,
                problem,
                attemptId,
                writePatchLog: true
            );
            return false;
        }
    }

    private void FinishFailedLaunchAttempt(
        LauncherStartGamePlan plan,
        string recordPhase,
        string recordDetail,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        bool preparedReadinessUsed,
        LauncherLaunchAttemptTiming timing,
        string markerPhase,
        string problem,
        string attemptId,
        bool writePatchLog
    )
    {
        LauncherLaunchMarkers.RecordPhase(recordPhase, recordDetail);
        LauncherLaunchMarkers.WriteLaunchAttempt(
            markerPhase,
            plan.Action,
            plan.Source,
            attemptId,
            readiness,
            modReadiness,
            preparedReadinessUsed,
            timing,
            problem
        );
        LauncherHandoffStateOwner.Shared.Fail(attemptId);
        _view.SetStatus(UserFacingLaunchProblem(markerPhase, problem), LauncherStatusSeverity.Error);
        _view.ShowHomeHelpAction();
        _view.AppendLog(problem);
        if (writePatchLog)
            PatchHelper.Log($"[Launcher] {problem}");
        SetLaunchInProgress(false);
    }

    private static string LaunchExceptionProblem(string phase, Exception exception)
    {
        var exceptionName = exception?.GetType().Name ?? "Exception";
        var message = CleanExceptionMessage(exception?.Message);
        return $"Launch blocked: {phase} ({exceptionName}: {message})";
    }

    private static string UserFacingLaunchProblem(string markerPhase, string fallback)
    {
        if (string.Equals(markerPhase, LauncherLaunchAttemptPhases.ModReadinessFailed, StringComparison.Ordinal))
            return "Mods could not be prepared. Try Play Vanilla. If that works, re-enable mods one at a time.";

        if (string.Equals(markerPhase, LauncherLaunchAttemptPhases.LaunchHandoffNotRequested, StringComparison.Ordinal)
            || string.Equals(markerPhase, LauncherLaunchAttemptPhases.LaunchHandoffFailed, StringComparison.Ordinal)
            || string.Equals(markerPhase, LauncherLaunchAttemptPhases.InProcessSignalFailed, StringComparison.Ordinal))
            return "Game files are ready, but the game did not appear. Return to the launcher and try Safe Start. If it happens again, create a support report.";

        if (string.Equals(markerPhase, LauncherLaunchAttemptPhases.ReadinessFailed, StringComparison.Ordinal)
            || string.Equals(markerPhase, LauncherLaunchAttemptPhases.Blocked, StringComparison.Ordinal)
            || string.Equals(markerPhase, LauncherLaunchAttemptPhases.BlockedInModel, StringComparison.Ordinal)
            || string.Equals(markerPhase, LauncherLaunchAttemptPhases.RestartRequestedWithoutReadyFiles, StringComparison.Ordinal))
            return "Game preparation failed. Repair selected branch, then try again. If it happens again, create a new support report.";

        if (string.Equals(markerPhase, LauncherLaunchAttemptPhases.SetupFailed, StringComparison.Ordinal))
            return "Launch setup failed. Try again. If it happens again, create a new support report.";

        return fallback;
    }

    private static string CleanExceptionMessage(string message)
        => string.IsNullOrWhiteSpace(message)
            ? "No exception message was provided."
            : message.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private string ActiveLaunchAttemptBranch()
        => !string.IsNullOrWhiteSpace(_activeLaunchAttempt?.Branch)
            ? _activeLaunchAttempt.Branch
            : SafeSelectedBranch();

    private static string SafeSelectedBranch()
    {
        try
        {
            return SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private void SetLaunchInProgress(bool inProgress)
    {
        _launchInProgress = inProgress;
        if (!inProgress)
            _activeLaunchAttempt = null;
        _view.SetLaunchControlsDisabled(inProgress);
    }

    internal void RestoreAfterFailedHandoff(string attemptId)
    {
        if (
            !_launchInProgress
            || _activeLaunchAttempt == null
            || !string.Equals(
                _activeLaunchAttempt.AttemptId,
                attemptId,
                StringComparison.Ordinal
            )
        )
            return;

        SetLaunchInProgress(false);
        _view.SetStatus(
            "The game did not appear. Try Safe Start. If it happens again, create a support report.",
            LauncherStatusSeverity.Warning
        );
        _view.ShowHomeHelpAction();
    }

}
