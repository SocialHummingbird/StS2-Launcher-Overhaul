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

    private readonly struct Snapshot
    {
        public Snapshot(
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

        public string DataDir { get; }
        public string AccountName { get; }
        public bool HasSavedCredentials { get; }
        public bool GameFilesReady { get; }
        public string SessionState { get; }
        public string FailReason { get; }
    }

    internal static string WriteLauncherDiagnosticsReport(
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
        => WriteLauncherDiagnosticsReport(
            CreateLauncherSnapshot(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            )
        );

    internal static string BuildLauncherDiagnosticsSummary(
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
        => BuildLauncherDiagnosticsSummary(
            CreateLauncherSnapshot(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            )
        );

    internal static string BuildLauncherRawErrorLog(
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
        => BuildLauncherRawErrorLog(
            CreateLauncherSnapshot(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            )
        );

    private static Snapshot CreateLauncherSnapshot(
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

    private static string BuildLauncherDiagnosticsReport(Snapshot snapshot)
        => BuildReportText(
            "STS2 Launcher diagnostics",
            "Generated UTC",
            AppendFullLauncherDiagnostics,
            snapshot
        );

    private static string BuildLauncherDiagnosticsSummary(Snapshot snapshot)
        => BuildLauncherErrorReport(
            snapshot,
            "=== LAST ERROR SUMMARY ===",
            AppendCompactLauncherState,
            AppendSummaryErrorDiagnostics,
            "=== END LAST ERROR SUMMARY ==="
        );

    private static string BuildLauncherRawErrorLog(Snapshot snapshot)
        => BuildLauncherErrorReport(
            snapshot,
            "=== RAW ERROR LOG ===",
            AppendDetailedLauncherState,
            AppendRawErrorDiagnostics,
            "=== END RAW ERROR LOG ==="
        );

    private static string BuildLauncherErrorReport(
        Snapshot snapshot,
        string title,
        Action<StringBuilder, Snapshot> appendState,
        Action<StringBuilder, string> appendDiagnostics,
        string endMarker
    )
        => BuildReportText(
            title,
            "UTC",
            (sb, current) => AppendLauncherErrorReport(
                sb,
                current,
                appendState,
                appendDiagnostics,
                endMarker
            ),
            snapshot
        );

    private static string BuildReportText<TContext>(
        string title,
        string generatedAtLabel,
        Action<StringBuilder, TContext> appendBody,
        TContext context
    )
    {
        var sb = StartTimestampedText(title, generatedAtLabel);
        appendBody(sb, context);
        return sb.ToString();
    }

    private static string WriteLauncherDiagnosticsReport(Snapshot snapshot)
        => WriteTimestampedReport(
            "sts2-launcher-diagnostics",
            snapshot.DataDir,
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
        => BuildReportText(
            "STS2 startup recovery diagnostics",
            "Generated UTC",
            AppendStartupRecoveryDiagnostics,
            dataDir
        );

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
    {
        AppendDetailedLauncherState(sb, snapshot);
        AppendLauncherPreferences(sb);
        AppendFullReportDiagnostics(sb, snapshot.DataDir);
    }

    private static void AppendLauncherErrorReport(
        StringBuilder sb,
        Snapshot snapshot,
        Action<StringBuilder, Snapshot> appendState,
        Action<StringBuilder, string> appendDiagnostics,
        string endMarker
    )
    {
        appendState(sb, snapshot);
        AppendPreviousLaunchPhase(sb, "Previous launch phase");
        appendDiagnostics(sb, snapshot.DataDir);
        sb.AppendLine(endMarker);
    }

    private static void AppendCompactLauncherState(StringBuilder sb, Snapshot snapshot)
    {
        sb.AppendLine($"Game files ready: {snapshot.GameFilesReady}");
        sb.AppendLine($"Session state: {ValueOrMissing(snapshot.SessionState)}");
    }

    private static void AppendDetailedLauncherState(StringBuilder sb, Snapshot snapshot)
    {
        sb.AppendLine($"Data dir: {ValueOrMissing(snapshot.DataDir)}");
        sb.AppendLine($"Account: {ValueOrMissing(snapshot.AccountName)}");
        sb.AppendLine($"Has saved credentials: {snapshot.HasSavedCredentials}");
        AppendCompactLauncherState(sb, snapshot);
        sb.AppendLine($"Fail reason: {ValueOrMissing(snapshot.FailReason)}");
    }

    private static void AppendLauncherPreferences(StringBuilder sb)
    {
        sb.AppendLine($"Cloud sync pref: {LauncherPreferences.ReadCloudSyncEnabled()}");
        sb.AppendLine($"Local backup pref: {LauncherPreferences.ReadLocalBackupEnabled()}");
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

    private static IEnumerable<(string Label, string Path)> StartupRecoveryFiles(string dataDir)
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
