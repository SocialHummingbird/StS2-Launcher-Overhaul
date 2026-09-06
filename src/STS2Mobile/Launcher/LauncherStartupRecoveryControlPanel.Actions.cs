using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static readonly RecoveryAction ExportDiagnosticsAction = new(
        "support report export",
        "Support report failed",
        ExportDiagnosticsReport
    );

    private static readonly RecoveryAction CopyRawErrorLogAction = new(
        "launcher log copy",
        "Launcher log copy failed",
        CopyRawErrorLogReport
    );

    private readonly struct RecoveryAction
    {
        internal RecoveryAction(
            string logAction,
            string failureTitle,
            Func<string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        private string LogAction { get; }
        private string FailureTitle { get; }
        private Func<string> Run { get; }

        internal void ShowResult(Label detail)
        {
            try
            {
                detail.Text = Run();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Startup recovery {LogAction} failed: {ex}");
                detail.Text = $"{FailureTitle}. Return to the launcher and try again.";
            }
        }
    }

    private void RestartWithSafeLaunch()
    {
        string attemptId = null;
        try
        {
            var dataDir = AppPaths.AppPrivateDataDir;
            var branch = LauncherPreferences.ReadGameBranch();
            var readiness = LauncherLaunchReadiness.Evaluate(
                dataDir,
                branch,
                "startup recovery safe-launch authorization"
            );
            var problem = readiness.ReadinessProblem;
            if (!readiness.Ready
                || !readiness.HasCurrentLaunchAuthorization(out problem))
            {
                PatchHelper.Log(
                    $"Startup recovery safe launch blocked: {problem ?? readiness.ReadinessProblem}"
                );
                _detail.Text = "Safe launch needs prepared game files. Returning to the launcher.";
                AndroidGodotAppBridge.RestartApp();
                return;
            }

            attemptId = LaunchAttemptContext.CreateAttemptId();
            if (!LauncherHandoffStateOwner.Shared.Begin(attemptId))
            {
                PatchHelper.Log(
                    "Startup recovery safe launch blocked by the authoritative handoff owner"
                );
                _detail.Text = "Safe restart is unavailable while another launch is active. Return to the launcher.";
                return;
            }

            var acceptance = AndroidGodotAppBridge.RequestLaunchRestart(
                LauncherRestartRequest.Create(attemptId, safe: true, readiness));
            if (!acceptance.Accepted)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(acceptance.Error)
                    ? "Android rejected the safe restart request." : acceptance.Error);
            LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
            LauncherLaunchMarkers.WriteLaunchAttempt(
                LauncherLaunchAttemptPhases.SafeAndroidRestartRequested,
                "safe",
                "startup-recovery-panel",
                attemptId,
                readiness,
                modReadiness: null,
                preparedReadinessUsed: true,
                LauncherLaunchAttemptTiming.NotMeasured(),
                "Startup recovery panel requested an authoritative safe restart"
            );
            _detail.Text = "Safe restart accepted. Restarting...";
            AndroidGodotAppBridge.FinishLaunchRestart(attemptId);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(attemptId))
                LauncherHandoffStateOwner.Shared.Fail(attemptId);
            _detail.Text = $"Safe restart failed: {ex.GetBaseException().Message}";
            PatchHelper.Log(
                $"Startup recovery safe launch authorization failed: {ex.GetBaseException().Message}"
            );
        }
    }

    private void ExportDiagnostics()
        => ShowRecoveryAction(ExportDiagnosticsAction);

    private void CopyRawErrorLog()
        => ShowRecoveryAction(CopyRawErrorLogAction);

    private void ShowRecoveryAction(RecoveryAction action)
        => action.ShowResult(_detail);
}
