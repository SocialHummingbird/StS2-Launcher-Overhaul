using System.Linq;
using System.Threading.Tasks;
using STS2Mobile.Steam;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal async Task StartWorkshopSyncAsync()
    {
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            RaiseWorkshopSyncFailed("Another Steam file operation is already running");
            return;
        }

        try
        {
            await RunWithDepotConnectionAsync(
                DepotConnectionAction.WorkshopSync(this)
            ).ConfigureAwait(false);
        }
        finally
        {
            run.Release();
        }
    }

    internal void ClearWorkshopMods()
    {
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            RaiseWorkshopClearFailed("Another Steam file operation is already running");
            return;
        }

        try
        {
            WorkshopModConsent.Clear();
            var stager = new SteamWorkshopStager();
            var removed = stager.ClearStagedMods();
            RaiseWorkshopSyncLogReceived(
                $"Workshop mods cleared: removed {removed} staged mod entries; Steam Cloud Push was not run."
            );
            RaiseWorkshopClearCompleted(removed);
        }
        catch (System.Exception ex)
        {
            RaiseWorkshopClearFailed(ex.GetBaseException().Message);
        }
        finally
        {
            run.Release();
        }
    }

    private async Task SyncWorkshopWithConnectionAsync(SteamConnection connection)
    {
        var service = new SteamWorkshopSyncService(connection);
        service.LogMessage += RaiseWorkshopSyncLogReceived;
        var manifest = await service.SyncAsync().ConfigureAwait(false);
        var summary = WorkshopSyncSummary(manifest);
        RaiseWorkshopSyncLogReceived(summary);
        RaiseWorkshopSyncCompleted(summary);
    }

    private static string WorkshopSyncSummary(SteamWorkshopSyncManifest manifest)
    {
        var staged = manifest.Items.Count(item => item.Status == "staged");
        var noPck = manifest.Items.Count(item => item.Status == "staged-no-pck");
        var unsupported = manifest.Items.Count(item => item.Status == "unsupported");
        var failed = manifest.Items.Count(item =>
            !string.IsNullOrWhiteSpace(item.Status)
            && item.Status.EndsWith("failed", System.StringComparison.OrdinalIgnoreCase)
        );
        var missingDependencies = manifest.MissingDependencyIds?.Count ?? manifest.MissingDependencyItemCount;
        var hasIssues = noPck > 0 || unsupported > 0 || failed > 0 || missingDependencies > 0;
        var prefix = hasIssues ? "Workshop mods need attention" : "Workshop mods synced";
        var detail = WorkshopIssueSummary(manifest);
        return string.IsNullOrWhiteSpace(detail)
            ? $"{prefix}: staged={staged}, noPck={noPck}, unsupported={unsupported}, failed={failed}, missingDeps={missingDependencies}"
            : $"{prefix}: staged={staged}, noPck={noPck}, unsupported={unsupported}, failed={failed}, missingDeps={missingDependencies}; {detail}";
    }

    private static string WorkshopIssueSummary(SteamWorkshopSyncManifest manifest)
    {
        var unsupported = manifest.Items
            .Where(item => item.Status == "unsupported")
            .Select(FormatWorkshopIssueItem)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(3)
            .ToArray();
        var failed = manifest.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Status)
                && item.Status.EndsWith("failed", System.StringComparison.OrdinalIgnoreCase))
            .Select(FormatWorkshopIssueItem)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(3)
            .ToArray();
        var parts = new System.Collections.Generic.List<string>();
        if (unsupported.Length > 0)
            parts.Add($"unsupported={string.Join(", ", unsupported)}");
        if (failed.Length > 0)
            parts.Add($"failedItems={string.Join(", ", failed)}");
        if (manifest.MissingDependencyIds?.Count > 0)
            parts.Add($"missingDeps={string.Join(", ", manifest.MissingDependencyIds.Take(5))}");

        return string.Join("; ", parts);
    }

    private static string FormatWorkshopIssueItem(SteamWorkshopSyncManifestItem item)
    {
        if (item == null || item.PublishedFileId == 0)
            return "";

        var title = string.IsNullOrWhiteSpace(item.Title)
            ? ""
            : $" {item.Title.Trim()}";
        return $"{item.PublishedFileId}{title}";
    }
}
