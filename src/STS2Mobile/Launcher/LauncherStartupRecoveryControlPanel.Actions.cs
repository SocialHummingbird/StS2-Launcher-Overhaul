using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => ShowActionResult(
            "diagnostics export",
            "Diagnostics export failed",
            session => session.ExportDiagnostics()
        );

    private void CopyRawErrorLog()
        => ShowActionResult(
            "raw error log copy",
            "Raw error log copy failed",
            session => session.CopyRawErrorLog()
        );

    private void ShowActionResult(
        string logAction,
        string failureTitle,
        Func<StartupRecoveryReportSession, string> run
    )
    {
        try
        {
            _detail.Text = run(StartupRecoveryReportSession.Capture());
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery {logAction} failed: {ex}");
            _detail.Text = $"{failureTitle}:\n{ex.GetBaseException().Message}";
        }
    }
}
