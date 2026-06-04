using System;
using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string MissingDiagnosticValue = "<none>";

    internal sealed class Snapshot
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

        internal Snapshot(
            string dataDir,
            string accountName,
            bool hasSavedCredentials,
            bool gameFilesReady,
            string sessionState,
            string failReason
        )
        {
            _state = new LauncherStateReport(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            );
        }

        private readonly LauncherStateReport _state;

        internal string BuildDiagnosticsSummary()
            => SummaryErrorReport.BuildText(this);

        internal string BuildRawErrorLog()
            => RawErrorReport.BuildText(this);

        internal string WriteDiagnosticsReport()
            => TimestampedReport.Launcher(
                _state.DataDir,
                AppendFullLauncherDiagnostics
            ).Write();

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
            => _state.AppendTo(sb, detail);

        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb);
            AppendFullReportDiagnostics(sb, _state.DataDir);
        }

        private void AppendErrorDiagnostics(
            StringBuilder sb,
            Action<StringBuilder, string> appendDiagnostics
        )
            => appendDiagnostics(sb, _state.DataDir);
    }

    internal readonly struct StartupRecoveryDiagnosticsReport
    {
        internal StartupRecoveryDiagnosticsReport(string dataDir)
        {
            DataDir = dataDir;
        }

        private string DataDir { get; }

        internal string Write()
            => CreateTimestampedReport().Write();

        internal string BuildText()
            => CreateTimestampedReport().BuildText();

        private TimestampedReport CreateTimestampedReport()
        {
            var dataDir = DataDir;
            return TimestampedReport.StartupRecovery(
                dataDir,
                sb => AppendStartupRecoveryDiagnostics(sb, dataDir)
            );
        }
    }

    internal static StartupRecoveryDiagnosticsReport StartupRecoveryReport(
        string dataDir
    )
        => new(dataDir);

    private static void AppendLauncherPreferences(StringBuilder sb)
    {
        var preferences = LauncherPreferences.ReadActionPreferences();
        sb.AppendLine($"Cloud sync pref: {preferences.CloudSyncEnabled}");
        sb.AppendLine($"Local backup pref: {preferences.LocalBackupEnabled}");
    }

    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        var phase = LauncherLaunchMarkers.ReadStartupPhase();
        if (!string.IsNullOrWhiteSpace(phase))
            sb.AppendLine($"{label}: {phase}");
    }

    private static string ValueOrMissing(string value)
        => string.IsNullOrWhiteSpace(value) ? MissingDiagnosticValue : value;

    private static void AppendStartupRecoveryDiagnostics(StringBuilder sb, string dataDir)
    {
        sb.AppendLine($"Data dir: {dataDir}");
        sb.AppendLine();

        foreach (var file in StartupRecoveryFiles(dataDir))
            AppendFileContentsSection(sb, file);

        AppendLogcatTail(
            sb,
            AndroidLogcatTail,
            StartupRecoveryTailLines
        );
    }

    private static IEnumerable<DiagnosticFile> StartupRecoveryFiles(string dataDir)
    {
        yield return StartupMarker(dataDir);
        yield return StartupSceneSnapshot(dataDir);
        yield return BootstrapTrace();
    }

}
