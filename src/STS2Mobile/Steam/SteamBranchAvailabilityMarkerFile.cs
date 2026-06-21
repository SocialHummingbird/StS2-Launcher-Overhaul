using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace STS2Mobile.Steam;

internal static class SteamBranchAvailabilityMarkerFile
{
    internal static string MarkerPath(string dataDir)
        => SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);

    internal static bool Exists(string dataDir)
        => File.Exists(MarkerPath(dataDir));

    internal static string[] ReadAllLines(string dataDir)
        => File.ReadAllLines(MarkerPath(dataDir));

    internal static string ReadValue(IEnumerable<string> lines, string prefix)
        => (lines ?? Array.Empty<string>())
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[prefix.Length..].Trim())
            .FirstOrDefault();

    internal static IEnumerable<string> ReadValues(IEnumerable<string> lines, string prefix)
        => (lines ?? Array.Empty<string>())
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[prefix.Length..].Trim());

    internal static string ReadValue(
        string dataDir,
        string prefix,
        string missingFileValue = null,
        string missingLineValue = null,
        string readFailedValue = null
    )
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return missingFileValue;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line[prefix.Length..].Trim();
            }
        }
        catch
        {
            return readFailedValue;
        }

        return missingLineValue;
    }

    internal static IReadOnlyList<string> ReadValues(
        string dataDir,
        string prefix,
        int maxValues = int.MaxValue
    )
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || maxValues <= 0)
                return Array.Empty<string>();

            var values = new List<string>();
            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                values.Add(line[prefix.Length..].Trim());
                if (values.Count >= maxValues)
                    break;
            }

            return values;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    internal static IEnumerable<SteamBranchAvailabilityMarkerRow> ReadVisibleRows(IEnumerable<string> lines)
        => ReadValues(lines, SteamBranchAvailabilityMarkerFields.VisibleBranch)
            .Select(SteamBranchAvailabilityMarkerRow.Parse)
            .Where(row => !row.IsEmpty);

    internal static IReadOnlyList<SteamBranchAvailabilityMarkerRow> ReadVisibleRows(
        string dataDir,
        int maxRows = int.MaxValue
    )
        => ReadValues(dataDir, SteamBranchAvailabilityMarkerFields.VisibleBranch, maxRows)
            .Select(SteamBranchAvailabilityMarkerRow.Parse)
            .Where(row => !row.IsEmpty)
            .ToArray();
}
