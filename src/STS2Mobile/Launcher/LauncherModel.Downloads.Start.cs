using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal async Task StartDownloadAsync()
    {
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            RaiseDownloadFailed("Download already running");
            return;
        }

        try
        {
            await RunWithDepotConnectionAsync(
                DepotConnectionAction.Download(this)
            );
        }
        finally
        {
            run.Release();
        }
    }

    internal Task CheckForUpdatesAsync()
        => RunWithDepotConnectionAsync(
            DepotConnectionAction.UpdateCheck(this)
        );

    internal Task RefreshBranchCatalogAsync()
        => RunWithDepotConnectionAsync(
            DepotConnectionAction.BranchCatalogRefresh(this)
        );
}
