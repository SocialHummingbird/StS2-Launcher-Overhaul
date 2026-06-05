using System;
using Godot;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private readonly struct RecoveryAction
    {
        private RecoveryAction(
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

        private void ShowResult(Label detail)
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

        internal static void ShowResult(
            Label detail,
            string logAction,
            string failureTitle,
            Func<string> run
        )
            => new RecoveryAction(logAction, failureTitle, run)
                .ShowResult(detail);
    }

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => RecoveryAction.ShowResult(
            _detail,
            "diagnostics export",
            "Diagnostics export failed",
            ExportDiagnosticsReport
        );

    private void CopyRawErrorLog()
        => RecoveryAction.ShowResult(
            _detail,
            "raw error log copy",
            "Raw error log copy failed",
            CopyRawErrorLogReport
        );
}
