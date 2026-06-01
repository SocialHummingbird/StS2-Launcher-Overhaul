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

    internal readonly struct Snapshot
    {
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

        internal string DataDir { get; }
        internal string AccountName { get; }
        internal bool HasSavedCredentials { get; }
        internal bool GameFilesReady { get; }
        internal string SessionState { get; }
        internal string FailReason { get; }
    }

    private static string BuildLauncherDiagnosticsReport(Snapshot snapshot)
    {
        var sb = StartTimestampedText("STS2 Launcher diagnostics", "Generated UTC");
        AppendFullLauncherDiagnostics(sb, snapshot);
        return sb.ToString();
    }

    internal static string BuildLauncherDiagnosticsSummary(Snapshot snapshot)
        => BuildLauncherDiagnosticsExtract(
            snapshot,
            "=== LAST ERROR SUMMARY ===",
            AppendCompactLauncherState,
            AppendSummaryErrorDiagnostics,
            "=== END LAST ERROR SUMMARY ==="
        );

    internal static string BuildLauncherRawErrorLog(Snapshot snapshot)
        => BuildLauncherDiagnosticsExtract(
            snapshot,
            "=== RAW ERROR LOG ===",
            AppendDetailedLauncherState,
            AppendRawErrorDiagnostics,
            "=== END RAW ERROR LOG ==="
        );

    private static string BuildLauncherDiagnosticsExtract(
        Snapshot snapshot,
        string title,
        Action<StringBuilder, Snapshot> appendState,
        Action<StringBuilder, string> appendDiagnostics,
        string footer
    )
    {
        var sb = StartTimestampedText(title, "UTC");
        appendState(sb, snapshot);
        AppendPreviousLaunchPhase(sb, "Previous launch phase");
        appendDiagnostics(sb, snapshot.DataDir);
        sb.AppendLine(footer);
        return sb.ToString();
    }

    internal static string WriteLauncherDiagnosticsReport(Snapshot snapshot)
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
    {
        AppendDetailedLauncherState(sb, snapshot);
        AppendLauncherPreferences(sb);
        AppendFullReportDiagnostics(sb, snapshot.DataDir);
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
