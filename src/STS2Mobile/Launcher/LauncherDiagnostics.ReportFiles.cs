using System.Collections.Generic;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendFullReportDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendDiagnosticReportFiles(sb, dataDir);
        AppendAndroidBridgeSection(sb, dataDir);
    }

    private static void AppendDiagnosticReportFiles(StringBuilder sb, string dataDir)
    {
        AppendFileSummaries(
            sb,
            DiagnosticReportFiles(dataDir),
            inlineContentLimit: 4096
        );

        foreach (var directory in DiagnosticReportDirectories(dataDir))
            AppendDirectoryListing(sb, directory);
    }

    private static IEnumerable<DiagnosticFile> DiagnosticReportFiles(string dataDir)
    {
        foreach (var file in StartupStateFiles(dataDir))
            yield return file;

        yield return ManualSafeLaunchMarker(dataDir);
        yield return BootstrapTrace();
        yield return GamePck(dataDir);
    }

    private static IEnumerable<DiagnosticFile> StartupStateFiles(string dataDir)
    {
        yield return StartupMarker(dataDir);
        yield return StartupSceneSnapshot(dataDir);
    }

    private static IEnumerable<DiagnosticDirectory> DiagnosticReportDirectories(
        string dataDir
    )
    {
        yield return new DiagnosticDirectory(
            "Game directory",
            Path.Combine(dataDir, LauncherStorageNames.GameDirectory),
            2
        );
        yield return new DiagnosticDirectory(
            "Download state",
            Path.Combine(dataDir, LauncherStorageNames.DownloadStateDirectory),
            1
        );
        yield return new DiagnosticDirectory(
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
}
