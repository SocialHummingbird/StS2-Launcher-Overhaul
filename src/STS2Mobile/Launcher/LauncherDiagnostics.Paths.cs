using System.IO;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static (string Label, string Path) AndroidUncaughtException(string dataDir)
        => (
            "Android uncaught exception",
            Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException)
        );

    private static (string Label, string Path) BootstrapTrace()
        => ("Bootstrap trace", BootstrapTraceFile.TracePath);

    private static (string Label, string Path) GamePck(string dataDir)
        => ("Game PCK", LauncherGameFiles.PckPath(dataDir));

    private static (string Label, string Path) ManualSafeLaunchMarker(string dataDir)
        => (
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static (string Label, string Path) StartupMarker(string dataDir)
        => (
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static (string Label, string Path) StartupSceneSnapshot(string dataDir)
        => (
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
