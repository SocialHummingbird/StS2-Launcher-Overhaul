using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int MaxReportDirectoriesPerDirectory = 40;
    private const int MaxReportFilesPerDirectory = 80;

    private static void AppendFullReportDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendDiagnosticReportFiles(sb, dataDir);
        AppendAndroidBridgeSection(sb, dataDir);
    }

    private static void AppendDiagnosticReportFiles(StringBuilder sb, string dataDir)
    {
        foreach (var file in DiagnosticReportFiles(dataDir))
        {
            AppendFileSummary(
                sb,
                file.Label,
                file.Path,
                inlineContentLimit: 4096
            );
        }

        foreach (var directory in DiagnosticReportDirectories(dataDir))
        {
            AppendDirectoryListing(
                sb,
                directory.Label,
                directory.Path,
                directory.MaxDepth
            );
        }
    }

    private static IEnumerable<FileReference> DiagnosticReportFiles(string dataDir)
    {
        yield return StartupMarker(dataDir);
        yield return StartupSceneSnapshot(dataDir);
        yield return ManualSafeLaunchMarker(dataDir);
        yield return BootstrapTrace();
        yield return GamePck(dataDir);
    }

    private static IEnumerable<(string Label, string Path, int MaxDepth)> DiagnosticReportDirectories(
        string dataDir
    )
    {
        yield return ("Game directory", Path.Combine(dataDir, LauncherStorageNames.GameDirectory), 2);
        yield return ("Download state", Path.Combine(dataDir, LauncherStorageNames.DownloadStateDirectory), 1);
        yield return (
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

    private static void AppendDirectoryListing(StringBuilder sb, string label, string path, int maxDepth)
    {
        sb.AppendLine($"{label}: {path}");
        try
        {
            if (!Directory.Exists(path))
            {
                sb.AppendLine("  exists=False");
                return;
            }

            AppendDirectoryTree(
                sb,
                path,
                depth: 0,
                maxDepth
            );
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }
    }

    private static void AppendDirectoryTree(
        StringBuilder sb,
        string path,
        int depth,
        int maxDepth
    )
    {
        if (depth > maxDepth)
            return;

        var indent = new string(' ', 2 + depth * 2);
        foreach (var dir in Directory
            .GetDirectories(path)
            .OrderBy(p => p)
            .Take(MaxReportDirectoriesPerDirectory))
        {
            sb.AppendLine($"{indent}[dir] {Path.GetFileName(dir)}");
            AppendDirectoryTree(sb, dir, depth + 1, maxDepth);
        }

        foreach (var filePath in Directory
            .GetFiles(path)
            .OrderBy(p => p)
            .Take(MaxReportFilesPerDirectory))
        {
            var file = new FileInfo(filePath);
            sb.AppendLine(
                $"{indent}{file.Name} bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}"
            );
        }
    }
}
