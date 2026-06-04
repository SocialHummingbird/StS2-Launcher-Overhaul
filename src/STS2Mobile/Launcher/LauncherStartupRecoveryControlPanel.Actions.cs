using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static readonly StartupRecoveryAction ExportDiagnosticsAction =
        StartupRecoveryAction.DiagnosticsExport(
            session => session.ExportDiagnostics()
        );

    private static readonly StartupRecoveryAction CopyRawErrorLogAction =
        StartupRecoveryAction.RawErrorLogCopy(
            session => session.CopyRawErrorLog()
        );

    private static void RestartWithSafeLaunch()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
    }

    private void ExportDiagnostics()
        => ShowActionResult(ExportDiagnosticsAction);

    private void CopyRawErrorLog()
        => ShowActionResult(CopyRawErrorLogAction);

    private void ShowActionResult(StartupRecoveryAction action)
        => _detail.Text = action.RunAndDescribe(
            StartupRecoveryReportSession.Capture()
        );
}
