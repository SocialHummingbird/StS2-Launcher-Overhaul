using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal static IReadOnlyList<BranchOption> ReadVisibleBranches(string dataDir)
    {
        if (!SteamBranchAvailabilityMarkerFile.Exists(dataDir))
            return Array.Empty<BranchOption>();

        try
        {
            return SteamBranchAvailabilityMarkerFile.ReadVisibleRows(dataDir)
                .Select(BranchOptionFromMarkerRow)
                .Where(option => !string.IsNullOrWhiteSpace(option.Branch))
                .GroupBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(option => string.Equals(option.Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<BranchOption>();
        }
    }

    internal static IReadOnlyList<string> ReadVisibleBranchNames(string dataDir)
        => ReadVisibleBranches(dataDir)
            .Select(option => option.Branch)
            .ToArray();

    internal static IReadOnlyList<BranchOption> ReadSelectableBranches(string dataDir)
    {
        var options = new List<BranchOption>();

        foreach (var branch in ReadVisibleBranches(dataDir))
            AddOrReplace(options, branch);

        foreach (var branch in ReadInstalledBranches(dataDir))
            AddIfMissing(options, branch);

        return options
            .OrderBy(option => string.Equals(option.Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string SourceDescription(string dataDir)
        => ReadVisibleBranches(dataDir).Count == 0
            ? "default options; no Steam app-info branch catalog captured yet"
            : "Steam app-info visible branch catalog";
}
