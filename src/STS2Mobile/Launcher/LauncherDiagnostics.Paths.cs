using System.IO;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct DiagnosticFile
    {
        internal DiagnosticFile(string label, string path)
        {
            Label = label;
            Path = path;
        }

        internal string Label { get; }
        internal string Path { get; }
    }

    private static DiagnosticFile AndroidUncaughtException(string dataDir)
        => new(
            "Android uncaught exception",
            Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException)
        );

    private static DiagnosticFile BootstrapTrace()
        => new("Bootstrap trace", BootstrapTraceFile.TracePath);

    private static DiagnosticFile GamePck(string dataDir)
        => new("Game PCK", LauncherGameFiles.PckPath(dataDir));

    private static DiagnosticFile ManualSafeLaunchMarker(string dataDir)
        => new(
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static DiagnosticFile StartupMarker(string dataDir)
        => new(
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static DiagnosticFile StartupSceneSnapshot(string dataDir)
        => new(
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
