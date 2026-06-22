using System;
using System.IO;
using System.Linq;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal static class LauncherWorkshopModSafety
{
    internal static bool HasActiveStagedMods()
        => ActiveStagedModCount() > 0;

    internal static int ActiveStagedModCount()
    {
        var rawStagedPckCount = RawStagedPckCount();
        try
        {
            if (!File.Exists(AppPaths.AppPrivateWorkshopManifestPath))
                return rawStagedPckCount;

            var manifest = new SteamWorkshopStager().LoadManifest();
            var manifestActiveCount = manifest.Items.Count(IsActiveStagedMod);
            return Math.Max(manifestActiveCount, rawStagedPckCount);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to inspect Workshop mod safety state: {ex.Message}");
            return rawStagedPckCount;
        }
    }

    private static bool IsActiveStagedMod(SteamWorkshopSyncManifestItem item)
        => item.HasPck && string.Equals(item.Status, "staged", StringComparison.OrdinalIgnoreCase);

    internal static int RawStagedPckCount()
    {
        try
        {
            return Directory.Exists(AppPaths.AppPrivateWorkshopStagedModsDir)
                ? Directory.EnumerateFiles(
                    AppPaths.AppPrivateWorkshopStagedModsDir,
                    "*.pck",
                    SearchOption.AllDirectories
                ).Count()
                : 0;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to scan raw staged Workshop PCK files: {ex.Message}");
            return 0;
        }
    }
}
