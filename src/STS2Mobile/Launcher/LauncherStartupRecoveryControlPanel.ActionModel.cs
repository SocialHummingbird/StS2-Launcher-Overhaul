using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
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
}
