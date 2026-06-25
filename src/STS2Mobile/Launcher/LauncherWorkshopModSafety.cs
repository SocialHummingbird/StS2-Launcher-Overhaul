using System;
using System.IO;
using System.Linq;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal static class LauncherWorkshopModSafety
{
    private const int MaxUnsupportedTitles = 2;

    internal static bool HasActiveStagedMods()
        => LauncherModSelectionState.PushShouldBeLocked();

    internal static int ActiveSelectedModCount()
        => LauncherModSelectionState.EnabledModCount();

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

    internal static int UnsupportedWorkshopItemCount()
        => InspectManifest(manifest => manifest.Items.Count(IsUnsupportedWorkshopItem), 0);

    internal static string UnsupportedWorkshopItemSummary()
        => InspectManifest(
            manifest =>
            {
                var items = manifest.Items
                    .Where(IsUnsupportedWorkshopItem)
                    .Select(FormatWorkshopItem)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Take(MaxUnsupportedTitles)
                    .ToArray();
                return string.Join(", ", items);
            },
            ""
        );

    internal static int ExternalManualModPckCount()
    {
        try
        {
            return Directory.Exists(AppPaths.ExternalModsDir)
                ? Directory.EnumerateFiles(
                    AppPaths.ExternalModsDir,
                    "*.pck",
                    SearchOption.AllDirectories
                ).Count()
                : 0;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to scan external manual mod PCK files: {ex.Message}");
            return 0;
        }
    }

    private static bool IsUnsupportedWorkshopItem(SteamWorkshopSyncManifestItem item)
        => string.Equals(item.Status, "unsupported", StringComparison.OrdinalIgnoreCase);

    private static string FormatWorkshopItem(SteamWorkshopSyncManifestItem item)
    {
        if (item == null || item.PublishedFileId == 0)
            return "";

        var title = string.IsNullOrWhiteSpace(item.Title)
            ? ""
            : $" {item.Title.Trim()}";
        return $"{item.PublishedFileId}{title}";
    }

    private static T InspectManifest<T>(Func<SteamWorkshopSyncManifest, T> inspect, T fallback)
    {
        try
        {
            if (!File.Exists(AppPaths.AppPrivateWorkshopManifestPath))
                return fallback;

            return inspect(new SteamWorkshopStager().LoadManifest());
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to inspect Workshop manifest state: {ex.Message}");
            return fallback;
        }
    }

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
