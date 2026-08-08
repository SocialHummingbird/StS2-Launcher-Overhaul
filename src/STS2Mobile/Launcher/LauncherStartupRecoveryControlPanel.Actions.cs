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
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => ShowRecoveryAction(ExportDiagnosticsAction);

    private void CopyRawErrorLog()
        => ShowRecoveryAction(CopyRawErrorLogAction);

    private void ShowRecoveryAction(RecoveryAction action)
        => action.ShowResult(_detail);
}
