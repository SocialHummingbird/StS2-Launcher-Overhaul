using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    private static IReadOnlyList<string> EnumerateBackups(string source)
    {
        var inspected = 0;
        if (!Directory.Exists(BackupDirectory))
            return Array.Empty<string>();

        var paths = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                BackupDirectory,
                $"*.{source}.bak",
                SearchOption.AllDirectories
            ))
            {
                if (inspected >= MaxBackupFilesToInspect)
                    break;

                inspected++;
                paths.Add(file);
            }
        }
        catch
        {
            return Array.Empty<string>();
        }

        return paths;
    }
}
