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

    private enum LauncherStateDetail
    {
        Compact,
        Detailed,
    }

    private readonly struct DiagnosticExtract
    {
        private DiagnosticExtract(
            string title,
            LauncherStateDetail stateDetail,
            Action<StringBuilder, string> appendDiagnostics,
            string footer
        )
        {
            Title = title;
            StateDetail = stateDetail;
            AppendDiagnostics = appendDiagnostics;
            Footer = footer;
        }

        private string Title { get; }
        private LauncherStateDetail StateDetail { get; }
        private Action<StringBuilder, string> AppendDiagnostics { get; }
        private string Footer { get; }

        internal static DiagnosticExtract Summary()
            => new(
                "=== LAST ERROR SUMMARY ===",
                LauncherStateDetail.Compact,
                AppendSummaryErrorDiagnostics,
                "=== END LAST ERROR SUMMARY ==="
            );

        internal static DiagnosticExtract RawErrorLog()
            => new(
                "=== RAW ERROR LOG ===",
                LauncherStateDetail.Detailed,
                AppendRawErrorDiagnostics,
                "=== END RAW ERROR LOG ==="
            );

        internal string Build(Snapshot snapshot)
        {
            var sb = StartTimestampedText(Title, "UTC");
            LauncherDiagnostics.AppendLauncherState(snapshot, sb, StateDetail);
            AppendPreviousLaunchPhase(sb, "Previous launch phase");
            LauncherDiagnostics.AppendErrorDiagnostics(snapshot, sb, AppendDiagnostics);
            sb.AppendLine(Footer);
            return sb.ToString();
        }
    }

    internal readonly struct Snapshot
    {
        private Snapshot(
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

        internal static Snapshot Create(
            string dataDir,
            string accountName,
            bool hasSavedCredentials,
            bool gameFilesReady,
            string sessionState,
            string failReason
        )
            => new(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            );

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

        private string WriteLauncherDiagnosticsReport(string report)
            => WriteTimestampedReport(
                "sts2-launcher-diagnostics",
                DataDir,
                report
            );
    }

    private static string BuildLauncherDiagnosticsReport(Snapshot snapshot)
    {
        var sb = StartTimestampedText("STS2 Launcher diagnostics", "Generated UTC");
        AppendFullLauncherDiagnostics(sb, snapshot);
        return sb.ToString();
    }

    internal static string BuildLauncherDiagnosticsSummary(Snapshot snapshot)
        => DiagnosticExtract.Summary().Build(snapshot);

    internal static string BuildLauncherRawErrorLog(Snapshot snapshot)
        => DiagnosticExtract.RawErrorLog().Build(snapshot);

    internal static string WriteLauncherDiagnosticsReport(Snapshot snapshot)
        => snapshot.WriteLauncherDiagnosticsReport(
            BuildLauncherDiagnosticsReport(snapshot)
        );

    internal static string WriteStartupRecoveryDiagnosticsReport(string dataDir)
        => WriteTimestampedReport(
            "sts2-startup-recovery-diagnostics",
            dataDir,
            BuildStartupRecoveryReport(dataDir)
        );

    private static string WriteTimestampedReport(
        string fileNamePrefix,
        string fallbackDirectory,
        string report
    )
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var targetPath = TryGetExternalDiagnosticsPath(fileName)
            ?? Path.Combine(fallbackDirectory, fileName);

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllText(targetPath, report);
        return targetPath;
    }

    internal static string BuildStartupRecoveryReport(string dataDir)
    {
        var sb = StartTimestampedText("STS2 startup recovery diagnostics", "Generated UTC");
        AppendStartupRecoveryDiagnostics(sb, dataDir);
        return sb.ToString();
    }

    private static StringBuilder StartTimestampedText(
        string title,
        string generatedAtLabel
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"{generatedAtLabel}: {DateTime.UtcNow:O}");
        return sb;
    }

    private static void AppendFullLauncherDiagnostics(StringBuilder sb, Snapshot snapshot)
        => snapshot.AppendFullLauncherDiagnostics(sb);

    private static void AppendErrorDiagnostics(
        Snapshot snapshot,
        StringBuilder sb,
        Action<StringBuilder, string> appendDiagnostics
    )
        => snapshot.AppendErrorDiagnostics(sb, appendDiagnostics);

    private static void AppendLauncherState(
        Snapshot snapshot,
        StringBuilder sb,
        LauncherStateDetail detail
    )
        => snapshot.AppendLauncherState(sb, detail);

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
