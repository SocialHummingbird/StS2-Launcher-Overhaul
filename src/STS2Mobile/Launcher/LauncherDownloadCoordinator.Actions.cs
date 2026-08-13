using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    internal void UpdateSelectedVersionPressed()
    {
        _view.HideActions();
        _view.ShowDownloadAction("Update selected version");
        DownloadPressed();
    }

    internal void DownloadPressed()
    {
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var downloadProblem = LauncherBranchCatalog.SelectedOptionDownloadProblem(
            selectedBranch,
            LauncherBranchCatalog.ReadVisibleBranches(_model.DataDir)
        );
        if (LauncherGameFiles.HasBranchMetadataProblem(_model.DataDir, selectedBranch))
        {
            if (!string.IsNullOrWhiteSpace(downloadProblem))
            {
                _view.ShowConfirmation(
                    BlockedRedownloadConfirmationMessage + "\n\n" + downloadProblem,
                    () => ApplyRedownloadBlockedByBranchProblem(downloadProblem),
                    "Delete Cache",
                    "Keep Cache"
                );
                return;
            }

            _view.ShowConfirmation(
                RedownloadConfirmationMessage,
                ApplyRedownloadAndDownload,
                "Redownload Version",
                "Keep Files"
            );
            return;
        }

        if (!string.IsNullOrWhiteSpace(downloadProblem))
        {
            _view.SetStatus(downloadProblem, LauncherStatusSeverity.Warning);
            _view.AppendLog(downloadProblem);
            ShowDownloadReadyAction();
            return;
        }

        _ = DownloadAsync();
    }

    internal void RedownloadPressed()
        => _view.ShowConfirmation(
            RedownloadConfirmationMessage,
            ApplyRedownload,
            "Repair current version",
            "Keep Files"
        );

    internal void ClearCachedVersionsPressed()
        => _view.ShowConfirmation(
            "Remove old downloaded game versions?\nThis keeps the selected version and removes other downloaded version caches.",
            ClearCachedVersions,
            "Remove old versions",
            "Keep versions"
        );

    private void ClearCachedVersions()
    {
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var selectedVersion = SteamGameBranch.DisplayName(selectedBranch);
        var removed = LauncherGameFiles.DeleteInactiveVersionCaches(
            _model.DataDir,
            selectedBranch,
            out var removedRuntimePacks
        );
        _refreshGameBranchOptions();
        var message = $"Removed {removed} inactive cached game version(s) and {removedRuntimePacks} runtime pack cache(s). Selected version preserved: {selectedVersion}.";
        _view.SetStatus(message, LauncherStatusSeverity.Information);
        _view.AppendLog(message);
    }

    private void ApplyRedownload()
    {
        LauncherLaunchReadinessCache.Clear("selected version redownload requested");
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        DownloadViewUpdate.RedownloadApplied().Apply(_view, _launch);
    }

    private void ApplyRedownloadAndDownload()
    {
        LauncherLaunchReadinessCache.Clear("selected version redownload and download requested");
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        _view.SetStatus(
            "Selected game version metadata cache cleared. Rebuilding selected version from Steam...",
            LauncherStatusSeverity.Working
        );
        _view.AppendLog("Selected game version metadata cache cleared before replacement download.");
        _ = DownloadAsync();
    }

    private void ApplyRedownloadBlockedByBranchProblem(string downloadProblem)
    {
        LauncherLaunchReadinessCache.Clear("selected version cache cleared while download remains blocked");
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        _view.SetStatus(downloadProblem, LauncherStatusSeverity.Warning);
        _view.AppendLog("Selected game version cache cleared, but replacement download remains blocked by Steam branch availability evidence.");
        _view.AppendLog(downloadProblem);
        ShowDownloadReadyAction();
    }
}
