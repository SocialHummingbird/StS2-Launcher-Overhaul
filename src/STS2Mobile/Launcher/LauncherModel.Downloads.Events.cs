using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private void RaiseDownloadCompleted(string branch)
        => Raise(DownloadCompleted, branch, nameof(DownloadCompleted));

    private void RaiseDownloadCancelled(string branch)
        => Raise(DownloadCancelled, branch, nameof(DownloadCancelled));

    private void RaiseDownloadFailed(string branch, string message)
        => Raise(DownloadFailed, new LauncherBranchOperationFailure(branch, message), nameof(DownloadFailed));

    private void RaiseDownloadLogReceived(string message)
        => Raise(DownloadLogReceived, message, nameof(DownloadLogReceived));

    private void RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress progress)
        => Raise(DownloadProgressChanged, progress, nameof(DownloadProgressChanged));

    private void RaiseUpdateCheckCompleted(string branch, bool hasUpdate)
        => Raise(UpdateCheckCompleted, new LauncherUpdateCheckResult(branch, hasUpdate), nameof(UpdateCheckCompleted));

    private void RaiseUpdateCheckFailed(string branch, string message)
        => Raise(UpdateCheckFailed, new LauncherBranchOperationFailure(branch, message), nameof(UpdateCheckFailed));

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
