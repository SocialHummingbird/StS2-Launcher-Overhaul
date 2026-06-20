using System;
using System.Collections.Generic;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    private const string BranchMarkerBranchPrefix = "Branch:";

    private static IReadOnlyList<BranchOption> ReadInstalledBranches(string dataDir)
    {
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        if (!Directory.Exists(versionsDir))
            return Array.Empty<BranchOption>();

        var options = new List<BranchOption>();
        try
        {
            foreach (var slotDir in Directory.GetDirectories(versionsDir))
            {
                var markerPath = Path.Combine(
                    slotDir,
                    SteamGameInstallPaths.LegacyPublicGameDirectory,
                    SteamGameInstallPaths.BranchMarkerFileName
                );
                var branch = ReadMarkerValue(markerPath, BranchMarkerBranchPrefix);
                if (string.IsNullOrWhiteSpace(branch))
                    continue;

                branch = SteamGameBranch.Normalize(branch);
                if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                    continue;

                var expectedDirectoryName = SteamGameBranch.StateDirectoryName(branch);
                var actualDirectoryName = Path.GetFileName(slotDir);
                if (!string.Equals(actualDirectoryName, expectedDirectoryName, StringComparison.OrdinalIgnoreCase))
                    continue;

                AddIfMissing(options, new BranchOption(branch, source: "local install"));
            }
        }
        catch
        {
            return Array.Empty<BranchOption>();
        }

        return options;
    }

    private static string ReadMarkerValue(string path, string prefix)
        => LauncherMarkerFile.ReadValue(
            path,
            prefix,
            missingFileValue: string.Empty,
            missingLineValue: string.Empty,
            readFailedValue: string.Empty
        );
}
