using System;
using System.IO;
using System.Runtime.InteropServices;

namespace STS2Mobile;

internal static class BootstrapTrace
{
    private const int AndroidLogInfo = 4;
    private const string TracePath =
        "/data/user/0/com.sts2launcher.overhaul.fork.dev/files/sts2_bootstrap_trace.log";
    private const string ExternalTracePath =
        "/sdcard/Android/data/com.sts2launcher.overhaul.fork.dev/files/sts2_bootstrap_trace.log";

    public static void Log(string message)
    {
        _ = message;
    }

    [DllImport("liblog.so")]
    private static extern int __android_log_write(int priority, string tag, string text);
}
