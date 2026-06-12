using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _branchCatalogRefreshRunning;

    private void RunBranchCatalogRefresh()
        => _ = RunBranchCatalogRefreshAsync();

    private async Task RunBranchCatalogRefreshAsync()
    {
        if (_branchCatalogRefreshRunning)
            return;

        _branchCatalogRefreshRunning = true;
        _view.SetRefreshGameVersionsBusy(true);
        _view.SetStatus("Refreshing Steam game version list...");
        _view.AppendLog("Refreshing Steam game version list from Steam app-info. This does not download or modify game files.");

        try
        {
            await _model.RefreshBranchCatalogAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Refresh game versions failed: {ex}");
            FailBranchCatalogRefresh(ex.Message);
        }
        finally
        {
            _branchCatalogRefreshRunning = false;
            _view.SetRefreshGameVersionsBusy(false);
        }
    }

    private void CompleteBranchCatalogRefresh()
    {
        RefreshGameBranchOptions();
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences());
        _view.SetStatus("Steam game version list refreshed.");
        _view.AppendLog("Steam game version list refreshed from account-visible app-info metadata.");
    }

    private void FailBranchCatalogRefresh(string message)
    {
        RefreshGameBranchOptions();
        var compact = LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message);
        _view.SetStatus($"Could not refresh Steam game version list: {compact}");
        _view.AppendLog($"Could not refresh Steam game version list: {compact}");
    }
}
