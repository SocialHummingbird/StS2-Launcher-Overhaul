using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private sealed class ErrorReportPlan
        {
            private const string GeneratedAtLabel = "UTC";
            private const string PreviousLaunchPhaseLabel = "Previous launch phase";

            private readonly string _title;
            private readonly LauncherStateDetail _stateDetail;
            private readonly Action<StringBuilder, string> _appendDiagnostics;
            private readonly string _footer;

            private ErrorReportPlan(
                string title,
                LauncherStateDetail stateDetail,
                Action<StringBuilder, string> appendDiagnostics,
                string footer
            )
            {
                _title = title;
                _stateDetail = stateDetail;
                _appendDiagnostics = appendDiagnostics;
                _footer = footer;
            }

            internal static ErrorReportPlan Summary(
                Action<StringBuilder, string> appendDiagnostics
            )
                => new(
                    "=== LAST ERROR SUMMARY ===",
                    LauncherStateDetail.Compact,
                    appendDiagnostics,
                    "=== END LAST ERROR SUMMARY ==="
                );

            internal static ErrorReportPlan Raw(
                Action<StringBuilder, string> appendDiagnostics
            )
                => new(
                    "=== RAW ERROR LOG ===",
                    LauncherStateDetail.Detailed,
                    appendDiagnostics,
                    "=== END RAW ERROR LOG ==="
                );

            internal string BuildText(Snapshot snapshot)
                => BuildTimestampedText(
                    _title,
                    GeneratedAtLabel,
                    sb => AppendBody(sb, snapshot)
                );

            private void AppendBody(StringBuilder sb, Snapshot snapshot)
            {
                snapshot.AppendLauncherState(sb, _stateDetail);
                AppendPreviousLaunchPhase(sb, PreviousLaunchPhaseLabel);
                snapshot.AppendErrorDiagnostics(sb, _appendDiagnostics);
                sb.AppendLine(_footer);
            }
        }
    }
}
