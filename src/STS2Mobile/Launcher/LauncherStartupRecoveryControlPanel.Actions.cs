using System;
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

    private sealed class StartupRecoveryAction
    {
        private StartupRecoveryAction(
            string logAction,
            string failureTitle,
            Func<StartupRecoveryReportSession, string> run
        )
        {
            LogAction = logAction;
            FailureTitle = failureTitle;
            Run = run;
        }

        private string LogAction { get; }
        private string FailureTitle { get; }
        private Func<StartupRecoveryReportSession, string> Run { get; }

        internal static StartupRecoveryAction DiagnosticsExport(
            Func<StartupRecoveryReportSession, string> run
        )
            => new(
                "diagnostics export",
                "Diagnostics export failed",
                run
            );

        internal static StartupRecoveryAction RawErrorLogCopy(
            Func<StartupRecoveryReportSession, string> run
        )
            => new(
                "raw error log copy",
                "Raw error log copy failed",
                run
            );

        internal string RunAndDescribe(StartupRecoveryReportSession session)
        {
            try
            {
                return Run(session);
            }
            catch (Exception ex)
            {
                return FailureMessage(ex);
            }
        }

        private string FailureMessage(Exception ex)
        {
            PatchHelper.Log($"Startup recovery {LogAction} failed: {ex}");
            return $"{FailureTitle}:\n{ex.GetBaseException().Message}";
        }
    }

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

    private static string ExportDiagnosticsMessage(string path, bool shared)
        => shared
            ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
            : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";

    private static string RawErrorLogCopiedMessage(int length)
        => $"Raw error log copied to clipboard.\n\nLength: {length:N0} characters";
}
