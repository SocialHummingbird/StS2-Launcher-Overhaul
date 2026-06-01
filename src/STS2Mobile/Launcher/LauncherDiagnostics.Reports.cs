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

    internal static string BuildLauncherDiagnosticsReport(
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
    {
        var sb = StartTimestampedText("STS2 Launcher diagnostics", "Generated UTC");
        AppendDetailedLauncherState(
            sb,
            dataDir,
            accountName,
            hasSavedCredentials,
            gameFilesReady,
            sessionState,
            failReason
        );
        AppendLauncherPreferences(sb);
        AppendFullReportDiagnostics(sb, dataDir);
        return sb.ToString();
    }

    internal static string BuildLauncherDiagnosticsSummary(
        string dataDir,
        bool gameFilesReady,
        string sessionState
    )
    {
        var sb = StartTimestampedText("=== LAST ERROR SUMMARY ===", "UTC");
        AppendCompactLauncherState(sb, gameFilesReady, sessionState);
        AppendPreviousLaunchPhase(sb, "Previous launch phase");
        AppendSummaryErrorDiagnostics(sb, dataDir);
        sb.AppendLine("=== END LAST ERROR SUMMARY ===");
        return sb.ToString();
    }

    internal static string BuildLauncherRawErrorLog(
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
    {
        var sb = StartTimestampedText("=== RAW ERROR LOG ===", "UTC");
        AppendDetailedLauncherState(
            sb,
            dataDir,
            accountName,
            hasSavedCredentials,
            gameFilesReady,
            sessionState,
            failReason
        );
        AppendPreviousLaunchPhase(sb, "Previous launch phase");
        AppendRawErrorDiagnostics(sb, dataDir);
        sb.AppendLine("=== END RAW ERROR LOG ===");
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

    internal static string WriteLauncherDiagnosticsReport(string dataDir, string report)
        => WriteTimestampedReport("sts2-launcher-diagnostics", dataDir, report);

    internal static string WriteStartupRecoveryDiagnosticsReport(string dataDir)
        => WriteStartupRecoveryDiagnosticsReport(dataDir, BuildStartupRecoveryReport(dataDir));

    private static string WriteStartupRecoveryDiagnosticsReport(string dataDir, string report)
        => WriteTimestampedReport("sts2-startup-recovery-diagnostics", dataDir, report);

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

    private static void AppendCompactLauncherState(
        StringBuilder sb,
        bool gameFilesReady,
        string sessionState
    )
    {
        sb.AppendLine($"Game files ready: {gameFilesReady}");
        sb.AppendLine($"Session state: {ValueOrMissing(sessionState)}");
    }

    private static void AppendDetailedLauncherState(
        StringBuilder sb,
        string dataDir,
        string accountName,
        bool hasSavedCredentials,
        bool gameFilesReady,
        string sessionState,
        string failReason
    )
    {
        sb.AppendLine($"Data dir: {ValueOrMissing(dataDir)}");
        sb.AppendLine($"Account: {ValueOrMissing(accountName)}");
        sb.AppendLine($"Has saved credentials: {hasSavedCredentials}");
        AppendCompactLauncherState(sb, gameFilesReady, sessionState);
        sb.AppendLine($"Fail reason: {ValueOrMissing(failReason)}");
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

    private static IEnumerable<FileReference> StartupRecoveryFiles(string dataDir)
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
