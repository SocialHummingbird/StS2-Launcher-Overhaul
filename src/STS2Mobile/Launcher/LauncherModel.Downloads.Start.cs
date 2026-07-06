using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal async Task StartDownloadAsync(string branch)
    {
        branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
        LauncherLaunchMarkers.RecordPhase(
            "download model start",
            $"branch={branch}"
        );
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            LauncherLaunchMarkers.RecordPhase("download model blocked", "Download already running");
            RaiseDownloadFailed(branch, "Download already running");
            return;
        }

        try
        {
            LauncherLaunchReadinessCache.Clear("download model started");
            await RunWithDepotConnectionAsync(
                DepotConnectionAction.Download(this, branch)
            );
        }
        finally
        {
            LauncherLaunchMarkers.RecordPhase("download model finished");
            run.Release();
        }
    }

    internal Task CheckForUpdatesAsync(string branch)
    {
        branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
        LauncherLaunchMarkers.RecordPhase(
            "update check model start",
            $"branch={branch}"
        );
        return RunWithDepotConnectionAsync(
            DepotConnectionAction.UpdateCheck(this, branch)
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
