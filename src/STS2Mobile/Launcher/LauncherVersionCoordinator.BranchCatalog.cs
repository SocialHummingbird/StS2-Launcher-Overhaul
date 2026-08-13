using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherVersionCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private bool _branchCatalogRefreshRunning;

    internal LauncherVersionCoordinator(LauncherModel model, LauncherView view)
    {
        _model = model;
        _view = view;
    }

    internal void RefreshGameBranchOptions()
    {
        _view.SetGameBranchOptions(ReadGameBranchOptions());
    }

    internal IReadOnlyList<LauncherBranchCatalog.BranchOption> ReadGameBranchOptions()
        => LauncherBranchCatalog.ReadSelectableBranches(_model.DataDir);

    internal void RunBranchCatalogRefresh()
        => _ = RunBranchCatalogRefreshAsync();

    private async Task RunBranchCatalogRefreshAsync()
    {
        if (_branchCatalogRefreshRunning)
            return;

        _branchCatalogRefreshRunning = true;
        _view.SetRefreshGameVersionsBusy(true);
        _view.SetStatus("Refreshing Steam game version list...", LauncherStatusSeverity.Working);
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

    internal void CompleteBranchCatalogRefresh()
    {
        var branches = LauncherBranchCatalog.ReadVisibleBranches(_model.DataDir);
        var preferences = LauncherPreferences.ReadActionPreferences();
        _view.SetActionPreferences(
            preferences,
            LauncherBranchCatalog.ReadSelectableBranches(_model.DataDir, branches)
        );
        var selectedBranch = preferences.GameBranch;
        var selectedVersion = STS2Mobile.Steam.SteamGameBranch.DisplayName(selectedBranch);
        var selectedStatus = LauncherBranchCatalog.SelectedOptionStatus(selectedBranch, branches);
        var selectedProblem = LauncherBranchCatalog.SelectedOptionDownloadProblem(selectedBranch, branches);

        _view.SetStatus(
            string.IsNullOrWhiteSpace(selectedProblem)
                ? $"Steam game version list refreshed. Selected version: {selectedVersion}."
                : selectedProblem,
            string.IsNullOrWhiteSpace(selectedProblem)
                ? LauncherStatusSeverity.Information
                : LauncherStatusSeverity.Warning
        );
        _view.AppendLog($"Steam game version list refreshed from account-visible app-info metadata. Selected version: {selectedVersion}. {selectedStatus}");
    }

    internal void FailBranchCatalogRefresh(string message)
    {
        RefreshGameBranchOptions();
        var compact = LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message);
        _view.SetStatus(
            $"Could not refresh Steam game version list: {compact}",
            LauncherStatusSeverity.Error
        );
        _view.AppendLog($"Could not refresh Steam game version list: {compact}");
    }
}
