using System;
using System.IO;
using System.Runtime.InteropServices;

namespace STS2Mobile;

internal static class BootstrapTrace
{
    private const string AndroidLogTag = "STS2Mobile";
    private const int AndroidLogInfoPriority = 4;
    private const string FileName = "sts2_bootstrap_trace.log";
    private const string FallbackPath =
        "/data/data/com.sts2launcher.overhaul.fork.dev/files/sts2_bootstrap_trace.log";
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

        var fileFailure = TryAppendTraceFile(line);
        if (fileFailure != null)
            TryWriteAndroidLog($"Bootstrap trace file sink failed: {fileFailure.Message}");
    }

    private static string GetTracePath()
    {
        try
        {
            return Path.Combine(Godot.OS.GetDataDir(), FileName);
        }
        catch
        {
            return FallbackPath;
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
