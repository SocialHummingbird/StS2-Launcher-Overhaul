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
        if (TryStartAutomaticRepair(selectedBranch))
            return;

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
            "Redownload Version",
            "Keep Version"
        );

    private void ApplyRedownload()
    {
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        DownloadViewUpdate.RedownloadApplied().Apply(_view, _launch);
    }

    private void ApplyRedownloadAndDownload()
    {
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
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        _view.SetStatus(downloadProblem, LauncherStatusSeverity.Warning);
        _view.AppendLog("Selected game version cache cleared, but replacement download remains blocked by Steam branch availability evidence.");
        _view.AppendLog(downloadProblem);
        ShowDownloadReadyAction();
    }
}
