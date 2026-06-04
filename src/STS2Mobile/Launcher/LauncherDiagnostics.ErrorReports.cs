using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private enum LauncherStateDetail
        {
            Compact,
            Detailed,
        }

        private readonly struct LauncherStateReport
        {
            internal LauncherStateReport(
                string dataDir,
                string accountName,
                bool hasSavedCredentials,
                bool gameFilesReady,
                string sessionState,
                string failReason
            )
            {
                DataDir = dataDir;
                AccountName = accountName;
                HasSavedCredentials = hasSavedCredentials;
                GameFilesReady = gameFilesReady;
                SessionState = sessionState;
                FailReason = failReason;
            }

            internal string DataDir { get; }
            private string AccountName { get; }
            private bool HasSavedCredentials { get; }
            private bool GameFilesReady { get; }
            private string SessionState { get; }
            private string FailReason { get; }

            internal void AppendTo(StringBuilder sb, LauncherStateDetail detail)
            {
                if (detail == LauncherStateDetail.Detailed)
                    AppendDetailedPrefix(sb);

                AppendCompact(sb);

                if (detail == LauncherStateDetail.Detailed)
                    sb.AppendLine($"Fail reason: {ValueOrMissing(FailReason)}");
            }

            private void AppendDetailedPrefix(StringBuilder sb)
            {
                sb.AppendLine($"Data dir: {ValueOrMissing(DataDir)}");
                sb.AppendLine($"Account: {ValueOrMissing(AccountName)}");
                sb.AppendLine($"Has saved credentials: {HasSavedCredentials}");
            }

            private void AppendCompact(StringBuilder sb)
            {
                sb.AppendLine($"Game files ready: {GameFilesReady}");
                sb.AppendLine($"Session state: {ValueOrMissing(SessionState)}");
            }
        }

        private static readonly ErrorReportPlan SummaryErrorReport =
            ErrorReportPlan.Summary(AppendSummaryErrorDiagnostics);

        private static readonly ErrorReportPlan RawErrorReport =
            ErrorReportPlan.Raw(AppendRawErrorDiagnostics);

        private sealed class ErrorReportPlan
        {
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
                    "UTC",
                    sb =>
                    {
                        snapshot.AppendLauncherState(sb, _stateDetail);
                        AppendPreviousLaunchPhase(sb, "Previous launch phase");
                        snapshot.AppendErrorDiagnostics(sb, _appendDiagnostics);
                        sb.AppendLine(_footer);
                    }
                );
        }

        internal string BuildDiagnosticsSummary()
            => SummaryErrorReport.BuildText(this);

        internal string BuildRawErrorLog()
            => RawErrorReport.BuildText(this);

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
            => _state.AppendTo(sb, detail);

        private void AppendErrorDiagnostics(
            StringBuilder sb,
            Action<StringBuilder, string> appendDiagnostics
        )
            => appendDiagnostics(sb, _state.DataDir);
    }
}
