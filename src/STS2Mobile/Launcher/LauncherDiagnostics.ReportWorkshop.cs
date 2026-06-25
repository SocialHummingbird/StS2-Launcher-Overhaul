using System;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendWorkshopDiagnostics(StringBuilder sb, string dataDir)
    {
        var downloadsDirectory = AppPaths.WorkshopDownloadsDir(dataDir);
        var stagedDirectory = AppPaths.WorkshopStagedModsDir(dataDir);
        var manifestPath = AppPaths.WorkshopManifestPath(dataDir);
        var clearMarkerPath = AppPaths.WorkshopClearMarkerPath(dataDir);
        var manifest = new SteamWorkshopStager(
            downloadsDirectory,
            stagedDirectory,
            manifestPath
        ).LoadManifest();

        sb.AppendLine($"Workshop sync manifest path: {manifestPath}");
        sb.AppendLine($"Workshop sync manifest present: {BoolText(File.Exists(manifestPath))}");
        sb.AppendLine($"Workshop sync manifest generated UTC: {ValueOrMissing(manifest.GeneratedAtUtc)}");
        sb.AppendLine($"Workshop clear marker path: {clearMarkerPath}");
        sb.AppendLine($"Workshop clear marker present: {BoolText(File.Exists(clearMarkerPath))}");
        sb.AppendLine($"Workshop cleared UTC: {ValueOrMissing(manifest.ClearedAtUtc)}");
        sb.AppendLine($"Workshop clear reason: {ValueOrMissing(manifest.ClearReason)}");
        sb.AppendLine($"Workshop downloads directory: {downloadsDirectory}");
        sb.AppendLine($"Workshop downloads directory present: {BoolText(Directory.Exists(downloadsDirectory))}");
        sb.AppendLine($"Workshop staged mods directory: {stagedDirectory}");
        sb.AppendLine($"Workshop staged mods directory present: {BoolText(Directory.Exists(stagedDirectory))}");
        sb.AppendLine($"Workshop subscription query type: {ValueOrMissing(manifest.SubscriptionQueryType)}");
        sb.AppendLine($"Workshop subscription query attempts: {ValueOrMissing(manifest.SubscriptionQueryAttempts)}");
        sb.AppendLine($"Workshop subscribed item count: {manifest.SubscribedItemCount}");
        sb.AppendLine($"Workshop dependency item count: {manifest.DependencyItemCount}");
        sb.AppendLine($"Workshop missing dependency item count: {manifest.MissingDependencyItemCount}");
        sb.AppendLine($"Workshop missing dependency ids: {IdList(manifest.MissingDependencyIds)}");
        sb.AppendLine($"Workshop total discovered item count: {manifest.TotalItemCount}");
        sb.AppendLine($"Workshop manifest item count: {manifest.Items.Count}");
        sb.AppendLine($"Workshop active staged PCK mod count: {manifest.Items.Count(IsActiveWorkshopMod)}");
        sb.AppendLine($"Workshop staged no-PCK item count: {manifest.Items.Count(item => HasStatus(item, "staged-no-pck"))}");
        sb.AppendLine($"Workshop unsupported item count: {manifest.Items.Count(item => HasStatus(item, "unsupported"))}");
        sb.AppendLine($"Workshop failed item count: {manifest.Items.Count(IsFailedWorkshopItem)}");
        sb.AppendLine($"Workshop raw staged directory count: {DirectoryCount(stagedDirectory)}");
        sb.AppendLine($"Workshop raw staged PCK file count: {RawStagedPckCount(stagedDirectory)}");
        sb.AppendLine($"Mod selector play mode: {(LauncherModSelectionState.IsModdedMode ? "modded" : "vanilla")}");
        sb.AppendLine($"Mod selector installed mod count: {LauncherModSelectionState.InstalledModCount()}");
        sb.AppendLine($"Mod selector enabled mod count: {LauncherModSelectionState.EnabledModCount()}");
        sb.AppendLine($"Workshop modded-save Cloud Push locked: {BoolText(LauncherWorkshopModSafety.HasActiveStagedMods())}");

        foreach (var item in manifest.Items.Take(32))
        {
            sb.AppendLine(
                "Workshop item: "
                + $"id={item.PublishedFileId} "
                + $"title=\"{item.Title}\" "
                + $"status={ValueOrMissing(item.Status)} "
                + $"hasPck={BoolText(item.HasPck)} "
                + $"files={item.FileCount} "
                + $"hash={ValueOrMissing(item.ContentSha256)} "
                + $"manifest={item.ManifestId} "
                + $"downloadSource={ValueOrMissing(item.DownloadSourceKind)} "
                + $"expectedBytes={item.ExpectedDownloadBytes} "
                + $"hcontent={item.HContentFile} "
                + $"reusedCache={BoolText(item.ReusedCachedDownload)} "
                + $"downloadUrlPresent={BoolText(item.DownloadUrlPresent)} "
                + $"downloadUrlHost={ValueOrMissing(item.DownloadUrlHost)} "
                + $"dependency={BoolText(item.IsDependency)} "
                + $"requiredBy=\"{IdList(item.RequiredByPublishedFileIds)}\" "
                + $"staged=\"{item.StagedDirectory}\" "
                + $"source=\"{item.SourceDirectory}\" "
                + $"error=\"{item.Error}\""
            );
        }
    }

    private static bool IsActiveWorkshopMod(SteamWorkshopSyncManifestItem item)
        => item.HasPck && HasStatus(item, "staged");

    private static bool HasStatus(SteamWorkshopSyncManifestItem item, string status)
        => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase);

    private static bool IsFailedWorkshopItem(SteamWorkshopSyncManifestItem item)
        => !string.IsNullOrWhiteSpace(item.Status)
            && item.Status.EndsWith("failed", StringComparison.OrdinalIgnoreCase);

    private static string IdList(System.Collections.Generic.IEnumerable<ulong> ids)
        => ids == null ? "" : string.Join(",", ids.Where(id => id != 0).OrderBy(id => id));

    private static int DirectoryCount(string directory)
    {
        try
        {
            return Directory.Exists(directory)
                ? Directory.EnumerateDirectories(directory).Count()
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int RawStagedPckCount(string stagedDirectory)
    {
        try
        {
            return Directory.Exists(stagedDirectory)
                ? Directory.EnumerateFiles(stagedDirectory, "*.pck", SearchOption.AllDirectories).Count()
                : 0;
        }
        catch
        {
            return 0;
        }
    }
}
