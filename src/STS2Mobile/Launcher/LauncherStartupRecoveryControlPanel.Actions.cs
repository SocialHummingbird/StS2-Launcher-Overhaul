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
        => ShowActionResult(StartupRecoveryAction.DiagnosticsExport);

    private void CopyRawErrorLog()
        => ShowActionResult(StartupRecoveryAction.RawErrorLogCopy);

    private void ShowActionResult(StartupRecoveryAction action)
        => _detail.Text = action.RunAndDescribe(
            StartupRecoveryReportSession.Capture()
        );
}
