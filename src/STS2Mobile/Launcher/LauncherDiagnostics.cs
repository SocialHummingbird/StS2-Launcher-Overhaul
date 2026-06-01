using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int AndroidBridgeTailLines = 500;
    private const int ErrorSummaryCaptureLines = 800;
    private const int ErrorSummaryFallbackLines = 40;
    private const int ErrorSummaryInterestingLines = 120;
    private const int StartupRecoveryTailLines = 500;

    private const string AndroidLogcatTail = "Android logcat tail:";
    private const string LogcatErrorLines = "Android logcat error lines:";
    private const string None = "<none>";
    private const string RawLogcatTail = "Android logcat tail (raw):";
    private const string SummaryFileStatusPrefix = "  ";
    private const string Unavailable = "<unavailable>";
    private const string Unknown = "<unknown>";

    private readonly struct FileReference
    {
        internal FileReference(string label, string path)
        {
            Label = label;
            Path = path;
        }

        internal string Label { get; }
        internal string Path { get; }
    }

    private static string AndroidAppVersion(string versionName)
        => $"Android app version: {versionName ?? Unknown}";

    private static string AndroidBridgeFailed(Exception ex)
        => $"Android bridge diagnostics failed: {ex.Message}";

    private static string ExternalFilesDir(string path)
        => $"External files dir: {path ?? None}";

    private static string Header(string label, string path)
        => $"{label}: {path}";

    private static string LogcatCollectionFailed(Exception ex)
        => $"<failed to collect logcat: {ex.Message}>";

    private static string UsableDataBytes(long bytes)
        => $"Usable data bytes: {bytes}";
}
