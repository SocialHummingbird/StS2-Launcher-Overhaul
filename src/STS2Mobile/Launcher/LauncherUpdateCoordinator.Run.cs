using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
{
    internal void RunUpdateCheck()
        => _ = RunUpdateCheckAsync();

    private async Task RunUpdateCheckAsync()
    {
        if (_updateCheckRunning)
            return;

        var branch = LauncherPreferences.ReadGameBranch();
        LauncherLaunchMarkers.RecordPhase(
            "update check requested",
            $"branch={branch}"
        );
        _updateCheckRunning = true;
        SetUpdateCheckBusy(busy: true);

        try
        {
            await CheckForUpdatesAsync(branch);
            LauncherLaunchMarkers.RecordPhase("update check completed", $"branch={branch}");
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("update check failed", ex.GetBaseException().Message);
            PatchHelper.Log($"[Launcher] Check for updates failed: {ex}");
            _versions.FailUpdateCheck(new LauncherBranchOperationFailure(branch, ex.Message));
        }
        finally
        {
            _updateCheckRunning = false;
            SetUpdateCheckBusy(busy: false);
        }
    }

    private void SetUpdateCheckBusy(bool busy)
        => _view.SetUpdateCheckBusy(busy);
}
