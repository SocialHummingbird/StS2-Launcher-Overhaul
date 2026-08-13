using System;
using System.Collections.Generic;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    private static IReadOnlyList<BranchOption> ReadInstalledBranches(string dataDir)
    {
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        var options = new List<BranchOption>();
        try
        {
            if (
                LauncherGameFiles.DownloadedForValidation(
                    dataDir,
                    SteamGameBranch.Public,
                    out _
                )
            )
            {
                AddIfMissing(
                    options,
                    new BranchOption(
                        SteamGameBranch.Public,
                        source: "local install",
                        isInstalled: true
                    )
                );
            }

            foreach (
                var slotDir in Directory.Exists(versionsDir)
                    ? Directory.GetDirectories(versionsDir)
                    : Array.Empty<string>()
            )
            {
                var markerPath = Path.Combine(
                    slotDir,
                    SteamGameInstallPaths.LegacyPublicGameDirectory,
                    SteamGameInstallPaths.BranchMarkerFileName
                );
                var branch = ReadMarkerValue(markerPath, LauncherBranchMarkerFields.Branch);
                if (string.IsNullOrWhiteSpace(branch))
                    continue;

                branch = SteamGameBranch.Normalize(branch);
                if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                    continue;

                var expectedDirectoryName = SteamGameBranch.StateDirectoryName(branch);
                var actualDirectoryName = Path.GetFileName(slotDir);
                if (!string.Equals(actualDirectoryName, expectedDirectoryName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!LauncherGameFiles.DownloadedForValidation(dataDir, branch, out _))
                    continue;

                AddIfMissing(
                    options,
                    new BranchOption(branch, source: "local install", isInstalled: true)
                );
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
