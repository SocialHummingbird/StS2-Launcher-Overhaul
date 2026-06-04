using System;
using System.IO;
using System.Linq;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int MaxReportDirectoriesPerDirectory = 40;
    private const int MaxReportFilesPerDirectory = 80;

    private readonly struct DiagnosticDirectory
    {
        internal DiagnosticDirectory(string label, string path, int maxDepth)
        {
            Label = label;
            Path = path;
            MaxDepth = maxDepth;
        }

        private string Label { get; }
        private string Path { get; }
        private int MaxDepth { get; }

        internal void AppendListing(StringBuilder sb)
        {
            sb.AppendLine($"{Label}: {Path}");
            try
            {
                if (!Directory.Exists(Path))
                {
                    sb.AppendLine("  exists=False");
                    return;
                }

                AppendDirectoryTree(
                    sb,
                    Path,
                    depth: 0,
                    maxDepth: MaxDepth
                );
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  failed={ex.Message}");
            }
        }
    }

    private static void AppendDirectoryListing(
        StringBuilder sb,
        DiagnosticDirectory directory
    )
        => directory.AppendListing(sb);

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
