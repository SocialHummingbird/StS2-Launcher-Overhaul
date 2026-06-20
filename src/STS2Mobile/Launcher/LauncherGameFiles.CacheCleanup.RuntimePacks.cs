using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private static string RuntimePackDirectoryPathForStateDirectory(string dataDir, string stateDirectoryName)
        => Path.Combine(dataDir, "runtime_packs", stateDirectoryName ?? string.Empty);

    private static int DeleteInactiveRuntimePacks(
        string dataDir,
        string selectedBranch,
        System.Collections.Generic.ICollection<string> markerLines
    )
    {
        var runtimePacksDir = Path.Combine(dataDir, "runtime_packs");
        if (!Directory.Exists(runtimePacksDir))
            return 0;

        var selectedStateDirectoryName = SteamGameBranch.StateDirectoryName(selectedBranch);
        var removed = 0;
        foreach (var runtimePackDirectory in Directory.GetDirectories(runtimePacksDir))
        {
            var directoryName = Path.GetFileName(runtimePackDirectory);
            if (string.Equals(directoryName, selectedStateDirectoryName, System.StringComparison.OrdinalIgnoreCase))
            {
                markerLines.Add($"Preserved selected runtime pack: {directoryName} -> {runtimePackDirectory}");
                continue;
            }

            DeleteDirectory(runtimePackDirectory);
            markerLines.Add($"Removed orphan runtime pack: {directoryName} -> {runtimePackDirectory} existsAfterDelete={Directory.Exists(runtimePackDirectory).ToString().ToLowerInvariant()}");
            removed++;
        }

        return removed;
    }
}
