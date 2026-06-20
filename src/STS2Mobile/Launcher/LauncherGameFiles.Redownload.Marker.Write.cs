using System;
using System.Collections.Generic;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private static void WriteRedownloadMarker(
        string dataDir,
        string selectedBranch,
        string gameDirectory,
        bool gameDirectoryExisted,
        bool? gameDirectoryExistsAfterDelete,
        string downloadStateDirectory,
        bool downloadStateDirectoryExisted,
        bool? downloadStateDirectoryExistsAfterDelete,
        string runtimePackDirectory,
        bool runtimePackDirectoryExisted,
        bool? runtimePackDirectoryExistsAfterDelete
    )
    {
        try
        {
            var lines = new List<string>
            {
                $"{RedownloadMarkerUtcPrefix} {DateTime.UtcNow:O}",
                $"{RedownloadMarkerSelectedBranchPrefix} {selectedBranch}",
                $"{RedownloadMarkerSelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}",
                $"{RedownloadMarkerVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}",
                $"{RedownloadMarkerVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}",
                $"{RedownloadMarkerGameDirectoryPrefix} {gameDirectory}",
                $"{RedownloadMarkerGameDirectoryExistedPrefix} {gameDirectoryExisted.ToString().ToLowerInvariant()}",
                $"{RedownloadMarkerDownloadStateDirectoryPrefix} {downloadStateDirectory}",
                $"{RedownloadMarkerDownloadStateDirectoryExistedPrefix} {downloadStateDirectoryExisted.ToString().ToLowerInvariant()}",
                $"{RedownloadMarkerRuntimePackDirectoryPrefix} {runtimePackDirectory}",
                $"{RedownloadMarkerRuntimePackDirectoryExistedPrefix} {runtimePackDirectoryExisted.ToString().ToLowerInvariant()}",
            };

            if (gameDirectoryExistsAfterDelete.HasValue)
                lines.Add($"{RedownloadMarkerGameDirectoryExistsAfterDeletePrefix} {gameDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            if (downloadStateDirectoryExistsAfterDelete.HasValue)
                lines.Add($"{RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix} {downloadStateDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            if (runtimePackDirectoryExistsAfterDelete.HasValue)
                lines.Add($"{RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix} {runtimePackDirectoryExistsAfterDelete.Value.ToString().ToLowerInvariant()}");

            File.WriteAllLines(RedownloadMarkerPath(dataDir), lines);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write game version redownload marker: {ex.Message}");
        }
    }
}
