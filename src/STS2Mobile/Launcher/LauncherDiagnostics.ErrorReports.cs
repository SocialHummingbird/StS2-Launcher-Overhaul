using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private const string ErrorReportGeneratedAtLabel = "UTC";
        private const string PreviousLaunchPhaseLabel = "Previous launch phase";

        internal string BuildDiagnosticsSummary()
            => BuildErrorReport(
                "=== LAST ERROR SUMMARY ===",
                LauncherStateDetail.Compact,
                AppendSummaryErrorDiagnostics,
                "=== END LAST ERROR SUMMARY ==="
            );

        internal string BuildRawErrorLog()
            => BuildErrorReport(
                "=== RAW ERROR LOG ===",
                LauncherStateDetail.Detailed,
                AppendRawErrorDiagnostics,
                "=== END RAW ERROR LOG ==="
            );

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
            => _state.AppendTo(sb, detail);

        private string BuildErrorReport(
            string title,
            LauncherStateDetail detail,
            Action<StringBuilder, string> appendDiagnostics,
            string footer
        )
            => BuildTimestampedText(
                title,
                ErrorReportGeneratedAtLabel,
                sb =>
                {
                    AppendLauncherState(sb, detail);
                    AppendPreviousLaunchPhase(sb, PreviousLaunchPhaseLabel);
                    appendDiagnostics(sb, _state.DataDir);
                    sb.AppendLine(footer);
                }
            );
    }
}
