using System.IO;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static FileReference AndroidUncaughtException(string dataDir)
        => new(
            "Android uncaught exception",
            Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException)
        );

    private static FileReference BootstrapTrace()
        => new("Bootstrap trace", BootstrapTraceFile.TracePath);

    private static FileReference GamePck(string dataDir)
        => new("Game PCK", LauncherGameFiles.PckPath(dataDir));

    private static FileReference ManualSafeLaunchMarker(string dataDir)
        => new(
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static FileReference StartupMarker(string dataDir)
        => new(
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static FileReference StartupSceneSnapshot(string dataDir)
        => new(
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
