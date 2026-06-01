using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int MaxLogcatLines = 1200;
    private const int MaxReportDirectoriesPerDirectory = 40;
    private const int MaxReportFilesPerDirectory = 80;
    private const int SmallAttachmentMaxChars = 64 * 1024;
    private static readonly string[] InterestingDiagnosticKeywords =
    {
        "error",
        "exception",
        "failed",
        "fatal",
        "crash",
        "watchdog",
        "stalled",
        "platformutil",
        "main menu",
        "startup",
        "godot",
        "mono",
        "sts2mobile",
    };

    internal string WriteDiagnosticsReport()
    {
        var report = BuildReport();
        return WriteReportFile(report);
    }

    internal string BuildDiagnosticsSummaryForDisplay()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== LAST ERROR SUMMARY ===");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Game files ready: {LauncherGameFiles.Ready(_dataDir)}");
        sb.AppendLine($"Session state: {_sessionState}");

        AppendPreviousLaunchPhase(sb, "Previous launch phase");

        foreach (var file in SummarySmallFiles())
        {
            sb.AppendLine();
            sb.AppendLine(LauncherDiagnosticAppendixText.Header(file.Label, file.Path));
            AppendTruncatedFile(
                sb,
                file.Path,
                file.MaxChars,
                LauncherDiagnosticAppendixText.SummaryFileStatusPrefix,
                LauncherDiagnosticAppendixText.SummaryFileStatusPrefix
            );
        }

        foreach (var tail in SummaryInterestingTails())
            AppendInterestingFileTail(sb, tail.Label, tail.Path, tail.MaxLines);

        AppendLogcatErrorSummary(sb);
        sb.AppendLine("=== END LAST ERROR SUMMARY ===");
        return sb.ToString();
    }

    internal string BuildRawErrorLogForClipboard()
    {
        var sb = new StringBuilder();
        var generatedUtc = DateTime.UtcNow;

        sb.AppendLine("=== RAW ERROR LOG ===");
        sb.AppendLine($"UTC: {generatedUtc:O}");
        sb.AppendLine($"Data dir: {_dataDir}");
        sb.AppendLine($"Account: {_credentialStore.AccountName ?? "<none>"}");
        sb.AppendLine($"Has saved credentials: {_credentialStore.HasCredentials}");
        sb.AppendLine($"Game files ready: {LauncherGameFiles.Ready(_dataDir)}");
        sb.AppendLine($"Session state: {_sessionState}");
        sb.AppendLine($"Fail reason: {_failReason ?? "<none>"}");

        AppendPreviousLaunchPhase(sb, "Previous launch phase");
        AppendFile(
            sb,
            "Startup marker",
            Path.Combine(_dataDir, LauncherStorageNames.StartupMarker),
            SmallAttachmentMaxChars
        );
        AppendFile(
            sb,
            "Android uncaught exception",
            Path.Combine(_dataDir, LauncherStorageNames.AndroidUncaughtException),
            SmallAttachmentMaxChars
        );
        AppendFile(sb, "Bootstrap trace", BootstrapTrace.TracePath, LargeAttachmentMaxChars);
        AppendFile(
            sb,
            "Startup scene snapshot",
            Path.Combine(_dataDir, LauncherStorageNames.StartupSceneSnapshot),
            LargeAttachmentMaxChars
        );
        AppendRawLogcatTail(sb, MaxLogcatLines);
        sb.AppendLine("=== END RAW ERROR LOG ===");
        return sb.ToString();
    }

    private string WriteReportFile(string report)
    {
        var fileName = $"sts2-launcher-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var targetPath = TryGetExternalDiagnosticsPath(fileName) ?? Path.Combine(_dataDir, fileName);

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllText(targetPath, report);
        PatchHelper.Log($"[Launcher] Diagnostics written to {targetPath}");
        return targetPath;
    }

    private string BuildReport()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDiagnosticReportFiles(sb, _dataDir);
        AppendAndroidBridgeSection(sb);
        return sb.ToString();
    }

    private void AppendHeader(StringBuilder sb)
    {
        var generatedUtc = DateTime.UtcNow;

        sb.AppendLine("STS2 Launcher diagnostics");
        sb.AppendLine($"Generated UTC: {generatedUtc:O}");
        sb.AppendLine($"Data dir: {_dataDir}");
        sb.AppendLine($"Account: {_credentialStore.AccountName ?? "<none>"}");
        sb.AppendLine($"Has saved credentials: {_credentialStore.HasCredentials}");
        sb.AppendLine($"Session state: {_sessionState}");
        sb.AppendLine($"Fail reason: {_failReason ?? "<none>"}");
        sb.AppendLine($"Game files ready: {LauncherGameFiles.Ready(_dataDir)}");
        sb.AppendLine($"Cloud sync pref: {LauncherPreferences.ReadCloudSyncEnabled()}");
        sb.AppendLine($"Local backup pref: {LauncherPreferences.ReadLocalBackupEnabled()}");
    }

    private void AppendAndroidBridgeSection(StringBuilder sb)
    {
        try
        {
            sb.AppendLine(
                LauncherDiagnosticAppendixText.AndroidAppVersion(
                    AndroidGodotAppBridge.GetVersionName()
                )
            );
            sb.AppendLine(
                LauncherDiagnosticAppendixText.ExternalFilesDir(
                    AndroidGodotAppBridge.GetExternalFilesDirPath()
                )
            );
            sb.AppendLine(
                LauncherDiagnosticAppendixText.UsableDataBytes(
                    AndroidGodotAppBridge.GetUsableSpaceBytes(_dataDir)
                )
            );
            sb.AppendLine();
            sb.AppendLine(LauncherDiagnosticAppendixText.AndroidLogcatTail);
            sb.AppendLine(
                AndroidGodotAppBridge.GetLogcatTail(
                    LauncherDiagnosticLogcatSettings.AndroidBridgeTailLines
                ) ?? LauncherDiagnosticAppendixText.Unavailable
            );
        }
        catch (Exception ex)
        {
            sb.AppendLine(LauncherDiagnosticAppendixText.AndroidBridgeFailed(ex));
        }
    }

    private static void AppendDiagnosticReportFiles(StringBuilder sb, string dataDir)
    {
        AppendPreviousLaunchPhase(sb, "Previous launch incomplete phase");

        foreach (var file in ReportFiles(dataDir))
            AppendFileInfo(sb, file.Label, file.Path);

        foreach (var directory in ReportDirectories(dataDir))
        {
            AppendDirectoryListing(
                sb,
                directory.Label,
                directory.Path,
                directory.MaxDepth
            );
        }
    }

    private static void AppendDirectoryListing(StringBuilder sb, string label, string path, int maxDepth)
    {
        sb.AppendLine($"{label}: {path}");
        try
        {
            if (!Directory.Exists(path))
            {
                sb.AppendLine("  exists=False");
                return;
            }

            AppendDirectoryTree(
                sb,
                path,
                depth: 0,
                maxDepth
            );
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }
    }

    private static void AppendDirectoryTree(
        StringBuilder sb,
        string path,
        int depth,
        int maxDepth
    )
    {
        if (depth > maxDepth)
            return;

        var indent = new string(' ', 2 + depth * 2);
        foreach (var dir in Directory
            .GetDirectories(path)
            .OrderBy(p => p)
            .Take(MaxReportDirectoriesPerDirectory))
        {
            sb.AppendLine($"{indent}[dir] {Path.GetFileName(dir)}");
            AppendDirectoryTree(sb, dir, depth + 1, maxDepth);
        }

        foreach (var filePath in Directory
            .GetFiles(path)
            .OrderBy(p => p)
            .Take(MaxReportFilesPerDirectory))
        {
            var file = new FileInfo(filePath);
            sb.AppendLine(
                $"{indent}{file.Name} bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}"
            );
        }
    }

    private static void AppendFileInfo(StringBuilder sb, string label, string path)
    {
        try
        {
            var file = new FileInfo(path);
            sb.AppendLine($"{label}: {path}");
            sb.AppendLine($"  exists={file.Exists}");
            if (file.Exists)
            {
                sb.AppendLine($"  bytes={file.Length}");
                sb.AppendLine($"  modifiedUtc={file.LastWriteTimeUtc:O}");
                if (file.Length <= 4096)
                    sb.AppendLine($"  contents={SingleLine(File.ReadAllText(path))}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{label}: failed to inspect {path}: {ex.Message}");
        }
    }

    private static IEnumerable<(string Label, string Path)> ReportFiles(string dataDir)
    {
        yield return ("Startup marker", Path.Combine(dataDir, LauncherStorageNames.StartupMarker));
        yield return (
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
        yield return ("Manual safe launch marker", LauncherLaunchMarkers.ManualSafeLaunchPath);
        yield return ("Bootstrap trace", BootstrapTrace.TracePath);
        yield return ("Game PCK", LauncherGameFiles.PckPath(dataDir));
    }

    private static IEnumerable<(string Label, string Path, int MaxDepth)> ReportDirectories(string dataDir)
    {
        yield return ("Game directory", Path.Combine(dataDir, LauncherStorageNames.GameDirectory), 2);
        yield return ("Download state", Path.Combine(dataDir, LauncherStorageNames.DownloadStateDirectory), 1);
        yield return (
            "Mono publish root",
            Path.Combine(
                dataDir,
                LauncherStorageNames.GodotDirectory,
                LauncherStorageNames.MonoDirectory,
                LauncherStorageNames.PublishDirectory
            ),
            2
        );
    }

    private static string SingleLine(string text)
        => text.Replace('\n', ' ').Replace('\r', ' ');

    private IEnumerable<(string Label, string Path, int MaxChars)> SummarySmallFiles()
    {
        yield return (
            "Startup marker",
            Path.Combine(_dataDir, LauncherStorageNames.StartupMarker),
            2048
        );
        yield return (
            "Android uncaught exception",
            Path.Combine(_dataDir, LauncherStorageNames.AndroidUncaughtException),
            4096
        );
    }

    private IEnumerable<(string Label, string Path, int MaxLines)> SummaryInterestingTails()
    {
        yield return (
            "Bootstrap trace",
            BootstrapTrace.TracePath,
            80
        );
        yield return (
            "Startup scene snapshot",
            Path.Combine(_dataDir, LauncherStorageNames.StartupSceneSnapshot),
            80
        );
    }

    private static void AppendFile(
        StringBuilder sb,
        string label,
        string path,
        int maxChars
    )
    {
        sb.AppendLine();
        sb.AppendLine(LauncherDiagnosticAppendixText.Header(label, path));
        AppendTruncatedFile(sb, path, maxChars);
    }

    private static void AppendLogcatErrorSummary(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine(LauncherDiagnosticAppendixText.LogcatErrorLines);
        var logcat = LauncherLogcatSnapshot.Capture(
            LauncherDiagnosticLogcatSettings.ErrorSummaryCaptureLines
        );
        if (!logcat.Available)
        {
            sb.AppendLine(logcat.FallbackText);
            return;
        }

        var lines = logcat.Text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(
            lines,
            LauncherDiagnosticLogcatSettings.ErrorSummaryInterestingLines
        ))
            sb.AppendLine(line);
    }

    private static void AppendRawLogcatTail(StringBuilder sb, int lineCount)
    {
        sb.AppendLine();
        sb.AppendLine(LauncherDiagnosticAppendixText.RawLogcatTail);
        var logcat = LauncherLogcatSnapshot.Capture(lineCount);
        sb.AppendLine(logcat.Content);
    }

    private static void AppendInterestingFileTail(
        StringBuilder sb,
        string label,
        string path,
        int maxLines
    )
    {
        sb.AppendLine();
        sb.AppendLine(LauncherDiagnosticAppendixText.Header(label, path));
        if (!TryRead(path, out var text, out var error))
        {
            sb.AppendLine(ReadStatus(error));
            return;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(lines, maxLines))
            sb.AppendLine(line);
    }

    private static string[] SelectInterestingDiagnosticLines(string[] lines, int maxLines)
    {
        var selected = Tail(lines.Where(IsInterestingDiagnosticLine), maxLines);

        if (selected.Length > 0)
            return selected;

        return Tail(
            lines.Where(line => !string.IsNullOrWhiteSpace(line)),
            Math.Min(maxLines, LauncherDiagnosticLogcatSettings.ErrorSummaryFallbackLines)
        );
    }

    private static bool IsInterestingDiagnosticLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var lower = line.ToLowerInvariant();
        return InterestingDiagnosticKeywords.Any(lower.Contains);
    }

    private static string[] Tail(IEnumerable<string> lines, int maxLines) =>
        lines.TakeLast(maxLines).ToArray();

    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        if (LauncherLaunchMarkers.PreviousGameLaunchIncomplete(out var phase))
            sb.AppendLine($"{label}: {phase ?? "<unknown>"}");
        else
            sb.AppendLine($"{label}: <none>");
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        string path,
        int maxChars,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        if (TryRead(path, out var text, out var error))
        {
            sb.AppendLine(TruncateForDisplay(text, maxChars));
            return;
        }

        sb.AppendLine(ReadStatus(error, missingPrefix, failedPrefix));
    }

    private static bool TryRead(string path, out string text, out string error)
    {
        try
        {
            if (!File.Exists(path))
            {
                text = null;
                error = null;
                return false;
            }

            text = File.ReadAllText(path);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            text = null;
            error = ex.Message;
            return false;
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }

    private static string ReadStatus(
        string error,
        string missingPrefix = "",
        string failedPrefix = ""
    )
        => error == null
            ? $"{missingPrefix}<missing>"
            : $"{failedPrefix}<failed to read: {error}>";

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        try
        {
            var externalDir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;

            var diagnosticsDir = Path.Combine(externalDir, "diagnostics");
            Directory.CreateDirectory(diagnosticsDir);
            return Path.Combine(diagnosticsDir, fileName);
        }
        catch
        {
            return null;
        }
    }
}
