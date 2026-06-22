using System;
using System.IO;
using System.Runtime.InteropServices;

namespace STS2Mobile;

internal static class BootstrapTrace
{
    private const string AndroidLogTag = "STS2Mobile";
    private const int AndroidLogInfoPriority = 4;
    private const string FileName = "sts2_bootstrap_trace.log";
    private const string AndroidTraceFileEnv = "STS2_ANDROID_TRACE_FILE";
    private const string TempDirectoryName = "tmp";
    private const long MaxBytes = 256L * 1024L;
    private static readonly object Lock = new();

    internal static string TracePath => GetTracePath();

    internal static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var line = FormatLine(message);
        var androidFailure = TryWriteAndroidLog(message);
        if (androidFailure != null)
            TryAppendTraceFile(FormatLine($"Bootstrap trace Android log sink failed: {androidFailure.Message}"));

        if (!ShouldAppendTraceFile())
            return;

        var fileFailure = TryAppendTraceFile(line);
        if (fileFailure != null)
            TryWriteAndroidLog($"Bootstrap trace file sink failed: {fileFailure.Message}");
    }

    private static bool ShouldAppendTraceFile()
        => !OperatingSystem.IsAndroid()
            || string.Equals(
                Environment.GetEnvironmentVariable(AndroidTraceFileEnv),
                "1",
                StringComparison.Ordinal
            );

    private static string GetTracePath()
    {
        try
        {
            return Path.Combine(Godot.OS.GetDataDir(), FileName);
        }
        catch
        {
            return Path.Combine(ResolveFallbackDataDirectory(), FileName);
        }
    }

    internal static string ResolveFallbackDataDirectory()
    {
        foreach (var variable in new[] { "TMPDIR", "TMP", "TEMP" })
        {
            var temp = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(temp))
                continue;

            if (!TryNormalizeDirectory(temp, out var normalized))
                continue;

            if (IsTempDirectoryName(normalized))
            {
                if (TryGetParentDirectory(normalized, out var parent))
                    return parent;
            }

            return normalized;
        }

        try
        {
            if (TryNormalizeDirectory(Path.GetTempPath(), out var tempPath))
                return tempPath;
        }
        catch
        {
        }

        return ".";
    }

    internal static bool TryNormalizeDirectory(string path, out string normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            normalized = Path.GetFullPath(path);
            return !string.IsNullOrWhiteSpace(normalized);
        }
        catch
        {
            normalized = null;
            return false;
        }
    }

    private static bool IsTempDirectoryName(string path)
    {
        try
        {
            return string.Equals(
                Path.GetFileName(path),
                TempDirectoryName,
                StringComparison.OrdinalIgnoreCase
            );
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetParentDirectory(string path, out string parent)
    {
        parent = null;
        try
        {
            parent = Path.GetDirectoryName(path);
            return !string.IsNullOrWhiteSpace(parent);
        }
        catch
        {
            parent = null;
            return false;
        }
    }

    private static string FormatLine(string message) =>
        $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";

    private static Exception TryWriteAndroidLog(string message)
    {
        try
        {
            __android_log_write(AndroidLogInfoPriority, AndroidLogTag, message);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static Exception TryAppendTraceFile(string line)
    {
        try
        {
            AppendTraceFile(line);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void AppendTraceFile(string line)
    {
        lock (Lock)
        {
            var path = TracePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            TrimTraceFileIfNeeded(path);
            File.AppendAllText(path, line);
        }
    }

    private static void TrimTraceFileIfNeeded(string path)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length < MaxBytes)
                return;

            var text = File.ReadAllText(path);
            var keepLength = (int)Math.Min(text.Length, MaxBytes / 2);
            File.WriteAllText(path, text.Substring(text.Length - keepLength));
        }
        catch
        {
        }
    }

    [DllImport("liblog.so")]
    private static extern int __android_log_write(int priority, string tag, string text);
}
