using System;
using System.IO;
using System.Runtime.InteropServices;

namespace STS2Mobile;

internal static class BootstrapTrace
{
    private const int AndroidLogInfo = 4;
    private const long MaxTraceBytes = 256L * 1024L;
    private static readonly object Lock = new();

    public static string TracePath
    {
        get
        {
            try
            {
                return Path.Combine(Godot.OS.GetDataDir(), "sts2_bootstrap_trace.log");
            }
            catch
            {
                return "/data/data/com.sts2launcher.overhaul.fork.dev/files/sts2_bootstrap_trace.log";
            }
        }
    }

    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
        try
        {
            __android_log_write(AndroidLogInfo, "STS2Mobile", message);
        }
        catch { }

        try
        {
            lock (Lock)
            {
                var path = TracePath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                TrimIfNeeded(path);
                File.AppendAllText(path, line);
            }
        }
        catch { }
    }

    private static void TrimIfNeeded(string path)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length < MaxTraceBytes)
                return;

            var text = File.ReadAllText(path);
            var keepLength = (int)Math.Min(text.Length, MaxTraceBytes / 2);
            File.WriteAllText(path, text.Substring(text.Length - keepLength));
        }
        catch { }
    }

    [DllImport("liblog.so")]
    private static extern int __android_log_write(int priority, string tag, string text);
}
