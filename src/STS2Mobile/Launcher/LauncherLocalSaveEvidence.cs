using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static class LauncherLocalSaveEvidence
{
    private const int MaxFilesToInspect = 1000;
    private const int MaxDirectoriesToInspect = 250;

    private static readonly string[] IgnoredDirectoryNames =
    {
        ".godot",
        "cache",
        LauncherStorageNames.DownloadStateDirectory,
        LauncherStorageNames.GameDirectory,
        LauncherStorageNames.GameVersionsDirectory,
        LauncherStorageNames.MonoDirectory,
        LauncherStorageNames.PublishDirectory,
        "tmp",
    };

    internal static bool HasImportantSaveEvidence(string dataDir)
        => CountImportantSaveEvidence(dataDir) > 0;

    internal static int CountImportantSaveEvidence(string dataDir)
    {
        if (string.IsNullOrWhiteSpace(dataDir) || !Directory.Exists(dataDir))
            return 0;

        var count = 0;
        foreach (var file in EnumerateFilesSafely(dataDir))
        {
            if (IsImportantSaveEvidence(dataDir, file))
                count++;
        }

        return count;
    }

    private static bool IsImportantSaveEvidence(string dataDir, string file)
    {
        var relativePath = Path.GetRelativePath(dataDir, file).Replace('\\', '/');
        var lowerPath = relativePath.ToLowerInvariant();
        var name = Path.GetFileName(lowerPath);

        return FileHasContent(file)
            && (
                lowerPath.EndsWith(".save", StringComparison.Ordinal)
                || lowerPath.EndsWith(".save.backup", StringComparison.Ordinal)
                || lowerPath.EndsWith(".run", StringComparison.Ordinal)
                || lowerPath.EndsWith(".bak", StringComparison.Ordinal)
                || string.Equals(name, "prefs", StringComparison.Ordinal)
                || string.Equals(name, "prefs.save", StringComparison.Ordinal)
                || string.Equals(name, "prefs.backup", StringComparison.Ordinal)
                || string.Equals(name, "prefs.save.backup", StringComparison.Ordinal)
            );
    }

    private static bool FileHasContent(string file)
    {
        try
        {
            return new FileInfo(file).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string[] EnumerateFilesSafely(string dataDir)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(dataDir);
        var inspectedDirectories = 0;

        while (pending.Count > 0
            && files.Count < MaxFilesToInspect
            && inspectedDirectories < MaxDirectoriesToInspect)
        {
            var directory = pending.Pop();
            inspectedDirectories++;

            if (IsIgnoredRuntimeDirectory(directory))
                continue;

            foreach (var file in SafeFiles(directory))
            {
                files.Add(file);
                if (files.Count >= MaxFilesToInspect)
                    break;
            }

            foreach (var child in SafeDirectories(directory))
                pending.Push(child);
        }

        return files.ToArray();
    }

    private static string[] SafeFiles(string directory)
    {
        try
        {
            return Directory.GetFiles(directory);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string[] SafeDirectories(string directory)
    {
        try
        {
            return Directory.GetDirectories(directory);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsIgnoredRuntimeDirectory(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
        {
            foreach (var ignored in IgnoredDirectoryNames)
            {
                if (string.Equals(part, ignored, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
