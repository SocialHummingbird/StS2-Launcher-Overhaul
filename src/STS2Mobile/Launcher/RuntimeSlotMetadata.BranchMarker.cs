using System;
using System.IO;
using System.Linq;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimeSlotMetadata
{
    private static string ReadMarkerValue(string path, string prefix)
        => LauncherMarkerFile.ReadValue(
            path,
            prefix,
            missingFileValue: LauncherMarkerFile.MissingLineValue
        );

    private static string BuildDepotManifestFingerprint(string branchMarkerPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(branchMarkerPath) || !File.Exists(branchMarkerPath))
                return "<missing>";

            var manifestRows = File.ReadLines(branchMarkerPath)
                .Where(line => line.StartsWith(LauncherBranchMarkerFields.DepotManifestRow, StringComparison.OrdinalIgnoreCase))
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToArray();
            if (manifestRows.Length == 0)
                return "<missing>";

            return StableHash16(string.Join("\n", manifestRows));
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }
}
