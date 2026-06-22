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
        yield return new DiagnosticFile(
            "Last game branch switch",
            LauncherBranchSwitchSafety.MarkerPath(dataDir)
        );
        yield return new DiagnosticFile(
            "Selected game branch marker",
            STS2Mobile.Steam.SteamGameInstallPaths.BranchMarkerPath(dataDir, LauncherPreferences.ReadGameBranch())
        );
        yield return new DiagnosticFile(
            "Workshop sync manifest",
            AppPaths.WorkshopManifestPath(dataDir)
        );
        yield return new DiagnosticFile(
            "Workshop clear marker",
            AppPaths.WorkshopClearMarkerPath(dataDir)
        );
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
            LauncherGameFiles.GameDirectoryPath(dataDir, LauncherPreferences.ReadGameBranch()),
            2
        );
        yield return new DiagnosticDirectory(
            "Game versions",
            Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory),
            2
        );
        yield return new DiagnosticDirectory(
            "Download state",
            STS2Mobile.Steam.SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, LauncherPreferences.ReadGameBranch()),
            1
        );
        yield return new DiagnosticDirectory(
            "Workshop staged mods",
            AppPaths.WorkshopStagedModsDir(dataDir),
            2
        );
        yield return new DiagnosticDirectory(
            "Workshop downloads",
            AppPaths.WorkshopDownloadsDir(dataDir),
            2
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
