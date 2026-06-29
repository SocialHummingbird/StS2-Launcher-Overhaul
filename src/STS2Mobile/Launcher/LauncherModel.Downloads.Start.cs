using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal async Task StartDownloadAsync()
    {
        LauncherLaunchMarkers.RecordPhase(
            "download model start",
            $"branch={LauncherPreferences.ReadGameBranch()}"
        );
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            LauncherLaunchMarkers.RecordPhase("download model blocked", "Download already running");
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
            LauncherLaunchMarkers.RecordPhase("download model finished");
            run.Release();
        }
    }

    internal Task CheckForUpdatesAsync()
    {
        LauncherLaunchMarkers.RecordPhase(
            "update check model start",
            $"branch={LauncherPreferences.ReadGameBranch()}"
        );
        return RunWithDepotConnectionAsync(
            DepotConnectionAction.UpdateCheck(this)
        );
    }

    internal Task RefreshBranchCatalogAsync()
    {
        LauncherLaunchMarkers.RecordPhase("branch catalog refresh model start");
        return RunWithDepotConnectionAsync(
            DepotConnectionAction.BranchCatalogRefresh(this)
        );
    }
}
