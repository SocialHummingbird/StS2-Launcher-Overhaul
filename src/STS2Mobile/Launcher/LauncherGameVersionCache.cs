using System;
using System.Collections.Generic;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherGameVersionCache
{
    internal readonly struct CachedVersion
    {
        internal CachedVersion(string directoryName, string path, bool selected)
        {
            DirectoryName = directoryName;
            Path = path;
            Selected = selected;
        }

        internal string DirectoryName { get; }
        internal string Path { get; }
        internal bool Selected { get; }
    }

    internal static IReadOnlyList<CachedVersion> Enumerate(string dataDir, string selectedBranch)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        if (!Directory.Exists(versionsDir))
            return Array.Empty<CachedVersion>();

        var selectedDirName = SteamGameBranch.StateDirectoryName(selectedBranch);
        var result = new List<CachedVersion>();
        foreach (var dir in Directory.GetDirectories(versionsDir))
        {
            var name = Path.GetFileName(dir);
            result.Add(
                new CachedVersion(
                    name,
                    dir,
                    string.Equals(name, selectedDirName, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        return result;
    }
}
