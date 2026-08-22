using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static readonly RecoveryAction ExportDiagnosticsAction = new(
        "help report export",
        "Help report failed",
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
                detail.Text = $"{FailureTitle}:\n{ex.GetBaseException().Message}";
            }
        }
    }

    private static void RestartWithSafeLaunch()
    {
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
                return;
            }

            LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
            AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        }
        catch (Exception ex)
        {
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
