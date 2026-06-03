using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string DiagnosticsDirectory = "diagnostics";
    private const string MissingDiagnosticValue = "<none>";

    private readonly struct TimestampedReport
    {
        private readonly string _fileNamePrefix;
        private readonly string _fallbackDirectory;
        private readonly string _title;
        private readonly string _generatedAtLabel;
        private readonly Action<StringBuilder> _appendBody;

        internal TimestampedReport(
            string fileNamePrefix,
            string fallbackDirectory,
            string title,
            string generatedAtLabel,
            Action<StringBuilder> appendBody
        )
        {
            _fileNamePrefix = fileNamePrefix;
            _fallbackDirectory = fallbackDirectory;
            _title = title;
            _generatedAtLabel = generatedAtLabel;
            _appendBody = appendBody;
        }

        internal string BuildText()
            => BuildTimestampedText(_title, _generatedAtLabel, _appendBody);

        internal string Write()
        {
            var fileName = $"{_fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
            var targetPath = TryGetExternalDiagnosticsPath(fileName)
                ?? Path.Combine(_fallbackDirectory, fileName);

            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllText(targetPath, BuildText());
            return targetPath;
        }
    }

    internal readonly struct Snapshot
    {
        private enum LauncherStateDetail
        {
            Compact,
            Detailed,
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
            DataDir = dataDir;
            AccountName = accountName;
            HasSavedCredentials = hasSavedCredentials;
            GameFilesReady = gameFilesReady;
            SessionState = sessionState;
            FailReason = failReason;
        }

        private string DataDir { get; }
        private string AccountName { get; }
        private bool HasSavedCredentials { get; }
        private bool GameFilesReady { get; }
        private string SessionState { get; }
        private string FailReason { get; }

        internal string BuildDiagnosticsSummary()
            => BuildDiagnosticText(
                "=== LAST ERROR SUMMARY ===",
                LauncherStateDetail.Compact,
                AppendSummaryErrorDiagnostics,
                "=== END LAST ERROR SUMMARY ==="
            );

        internal string BuildRawErrorLog()
            => BuildDiagnosticText(
                "=== RAW ERROR LOG ===",
                LauncherStateDetail.Detailed,
                AppendRawErrorDiagnostics,
                "=== END RAW ERROR LOG ==="
            );

        internal string WriteDiagnosticsReport()
            => new TimestampedReport(
                "sts2-launcher-diagnostics",
                DataDir,
                "STS2 Launcher diagnostics",
                "Generated UTC",
                AppendFullLauncherDiagnostics
            ).Write();

        private string BuildDiagnosticText(
            string title,
            LauncherStateDetail stateDetail,
            Action<StringBuilder, string> appendDiagnostics,
            string footer
        )
        {
            var snapshot = this;
            return BuildTimestampedText(
                title,
                "UTC",
                sb =>
                {
                    snapshot.AppendLauncherState(sb, stateDetail);
                    AppendPreviousLaunchPhase(sb, "Previous launch phase");
                    snapshot.AppendErrorDiagnostics(sb, appendDiagnostics);
                    sb.AppendLine(footer);
                }
            );
        }

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
        {
            if (detail == LauncherStateDetail.Detailed)
            {
                sb.AppendLine($"Data dir: {ValueOrMissing(DataDir)}");
                sb.AppendLine($"Account: {ValueOrMissing(AccountName)}");
                sb.AppendLine($"Has saved credentials: {HasSavedCredentials}");
            }

            AppendCompactLauncherState(sb);

            if (detail == LauncherStateDetail.Detailed)
                sb.AppendLine($"Fail reason: {ValueOrMissing(FailReason)}");
        }

        private void AppendCompactLauncherState(StringBuilder sb)
        {
            sb.AppendLine($"Game files ready: {GameFilesReady}");
            sb.AppendLine($"Session state: {ValueOrMissing(SessionState)}");
        }

        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb);
            AppendFullReportDiagnostics(sb, DataDir);
        }

        private void AppendErrorDiagnostics(
            StringBuilder sb,
            Action<StringBuilder, string> appendDiagnostics
        )
            => appendDiagnostics(sb, DataDir);
    }

    internal static string WriteStartupRecoveryDiagnosticsReport(string dataDir)
        => StartupRecoveryReport(dataDir).Write();

    internal static string BuildStartupRecoveryReport(string dataDir)
        => StartupRecoveryReport(dataDir).BuildText();

    private static TimestampedReport StartupRecoveryReport(string dataDir)
        => new(
            "sts2-startup-recovery-diagnostics",
            dataDir,
            "STS2 startup recovery diagnostics",
            "Generated UTC",
            sb => AppendStartupRecoveryDiagnostics(sb, dataDir)
        );

    private static string BuildTimestampedText(
        string title,
        string generatedAtLabel,
        Action<StringBuilder> appendBody
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"{generatedAtLabel}: {DateTime.UtcNow:O}");
        appendBody(sb);
        return sb.ToString();
    }

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

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        try
        {
            var externalDir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;

            var diagnosticsDir = Path.Combine(externalDir, DiagnosticsDirectory);
            Directory.CreateDirectory(diagnosticsDir);
            return Path.Combine(diagnosticsDir, fileName);
        }
        catch
        {
            return null;
        }
    }
}
