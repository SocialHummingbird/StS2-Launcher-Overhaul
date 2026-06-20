using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLocalSaveEvidence
{
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
