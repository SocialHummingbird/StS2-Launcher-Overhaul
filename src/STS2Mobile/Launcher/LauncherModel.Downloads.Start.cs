using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    internal Task StartDownloadAsync(string branch)
        => StartSelectedVersionOperationAsync(
            branch,
            automaticRepairOnly: false
        );

    internal Task StartAutomaticRepairAsync(string branch)
        => StartSelectedVersionOperationAsync(
            branch,
            automaticRepairOnly: true
        );

    private async Task StartSelectedVersionOperationAsync(
        string branch,
        bool automaticRepairOnly
    )
    {
        branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
        LauncherLaunchMarkers.RecordPhase(
            automaticRepairOnly
                ? "automatic PCK repair model start"
                : "download model start",
            $"branch={branch}; localRepairOnly={automaticRepairOnly}"
        );
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            LauncherLaunchMarkers.RecordPhase(
                automaticRepairOnly
                    ? "automatic PCK repair model blocked"
                    : "download model blocked",
                "Selected-version operation already running"
            );
            RaiseDownloadFailed(branch, "Selected-version operation already running");
            return;
        }

        try
        {
            LauncherLaunchReadinessCache.Clear(
                automaticRepairOnly
                    ? "automatic PCK repair model started"
                    : "download model started"
            );
            if (automaticRepairOnly)
            {
                await RunAutomaticPckRepairAsync(branch);
                return;
            }

            if (LocalPckRepairOperation.ClassifyForLauncherRouting(
                    _dataDir,
                    branch
                ) == InstalledGameVersionReadiness.AutomaticRepair)
            {
                await RunAutomaticPckRepairAsync(branch);
                return;
            }

            await RunWithDepotConnectionAsync(
                DepotConnectionAction.Download(this, branch)
            );
        }
        finally
        {
            LauncherLaunchMarkers.RecordPhase(
                automaticRepairOnly
                    ? "automatic PCK repair model finished"
                    : "download model finished"
            );
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
