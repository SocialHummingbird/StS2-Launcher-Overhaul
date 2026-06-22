using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private void RaiseDownloadCompleted()
        => Raise(DownloadCompleted, nameof(DownloadCompleted));

    private void RaiseDownloadCancelled()
        => Raise(DownloadCancelled, nameof(DownloadCancelled));

    private void RaiseDownloadFailed(string message)
        => Raise(DownloadFailed, message, nameof(DownloadFailed));

    private void RaiseDownloadLogReceived(string message)
        => Raise(DownloadLogReceived, message, nameof(DownloadLogReceived));

    private void RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress progress)
        => Raise(DownloadProgressChanged, progress, nameof(DownloadProgressChanged));

    private void RaiseUpdateCheckCompleted(bool hasUpdate)
        => Raise(UpdateCheckCompleted, hasUpdate, nameof(UpdateCheckCompleted));

    private void RaiseUpdateCheckFailed(string message)
        => Raise(UpdateCheckFailed, message, nameof(UpdateCheckFailed));

    private void RaiseBranchCatalogRefreshCompleted()
        => Raise(BranchCatalogRefreshCompleted, nameof(BranchCatalogRefreshCompleted));

    private void RaiseBranchCatalogRefreshFailed(string message)
        => Raise(BranchCatalogRefreshFailed, message, nameof(BranchCatalogRefreshFailed));

    private void RaiseWorkshopSyncLogReceived(string message)
        => Raise(WorkshopSyncLogReceived, message, nameof(WorkshopSyncLogReceived));

    private void RaiseWorkshopSyncCompleted(string summary)
        => Raise(WorkshopSyncCompleted, summary, nameof(WorkshopSyncCompleted));

    private void RaiseWorkshopSyncFailed(string message)
        => Raise(WorkshopSyncFailed, message, nameof(WorkshopSyncFailed));

    private void RaiseWorkshopClearCompleted(int removedCount)
        => Raise(WorkshopClearCompleted, removedCount, nameof(WorkshopClearCompleted));

    private void RaiseWorkshopClearFailed(string message)
        => Raise(WorkshopClearFailed, message, nameof(WorkshopClearFailed));
}
