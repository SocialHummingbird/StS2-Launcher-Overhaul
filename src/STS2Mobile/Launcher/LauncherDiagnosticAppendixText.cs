using System;

namespace STS2Mobile.Launcher;

internal static class LauncherDiagnosticAppendixText
{
    internal const string LogcatErrorLines = "Android logcat error lines:";
    internal const string RawLogcatTail = "Android logcat tail (raw):";
    internal const string AndroidLogcatTail = "Android logcat tail:";
    internal const string Unknown = "<unknown>";
    internal const string None = "<none>";
    internal const string SummaryFileStatusPrefix = "  ";
    internal const string Unavailable = "<unavailable>";

    internal static string AndroidAppVersion(string versionName)
        => $"Android app version: {versionName ?? Unknown}";

    internal static string ExternalFilesDir(string path)
        => $"External files dir: {path ?? None}";

    internal static string Header(string label, string path)
        => $"{label}: {path}";

    internal static string LogcatCollectionFailed(Exception ex)
        => $"<failed to collect logcat: {ex.Message}>";

    internal static string UsableDataBytes(long bytes)
        => $"Usable data bytes: {bytes}";

    internal static string AndroidBridgeFailed(Exception ex)
        => $"Android bridge diagnostics failed: {ex.Message}";
}
