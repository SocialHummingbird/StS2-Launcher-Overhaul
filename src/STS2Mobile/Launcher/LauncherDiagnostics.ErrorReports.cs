using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private const string ErrorReportGeneratedAtLabel = "UTC";
        private const string PreviousLaunchPhaseLabel = "Previous launch phase";

        private readonly struct ErrorReportDefinition
        {
            private ErrorReportDefinition(
                string title,
                LauncherStateDetail stateDetail,
                Action<StringBuilder> appendDiagnostics,
                string footer
            )
            {
                Title = title;
                StateDetail = stateDetail;
                AppendDiagnostics = appendDiagnostics;
                Footer = footer;
            }

            internal string Title { get; }
            internal LauncherStateDetail StateDetail { get; }
            internal Action<StringBuilder> AppendDiagnostics { get; }
            internal string Footer { get; }

            internal static ErrorReportDefinition Summary(
                string dataDir
            )
                => new(
                    "=== LAST ERROR SUMMARY ===",
                    LauncherStateDetail.Compact,
                    sb => AppendSummaryErrorDiagnostics(sb, dataDir),
                    "=== END LAST ERROR SUMMARY ==="
                );

            internal static ErrorReportDefinition RawLog(
                string dataDir
            )
                => new(
                    "=== RAW ERROR LOG ===",
                    LauncherStateDetail.Detailed,
                    sb => AppendRawErrorDiagnostics(sb, dataDir),
                    "=== END RAW ERROR LOG ==="
                );
        }

        internal string BuildDiagnosticsSummary()
            => BuildErrorReport(ErrorReportDefinition.Summary(_state.DataDir));

        internal string BuildRawErrorLog()
            => BuildErrorReport(ErrorReportDefinition.RawLog(_state.DataDir));

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
            => _state.AppendTo(sb, detail);

        private string BuildErrorReport(
            ErrorReportDefinition report
        )
            => CreateTimestampedText(
                report.Title,
                ErrorReportGeneratedAtLabel,
                sb =>
                {
                    AppendLauncherState(sb, report.StateDetail);
                    AppendPreviousLaunchPhase(sb, PreviousLaunchPhaseLabel);
                    report.AppendDiagnostics(sb);
                    sb.AppendLine(report.Footer);
                }
            ).Build();
    }
}
