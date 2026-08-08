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
            _view.SetStatus("Start Game is already running. Waiting for launch handoff...");
            return false;
        }

        SetLaunchInProgress(true);
        var attemptId = LaunchAttemptContext.CreateAttemptId();
        var attemptTimer = Stopwatch.StartNew();
        try
        {
            var localBackupRefresh =
                CloudSyncCoordinator.RefreshLocalBackup();
            _localBackupRecoveryCompleted(localBackupRefresh);
            var branch = SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
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
        _view.SetStatus(problem);
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

}
