namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    internal void UpdateSelectedVersionPressed()
    {
        _view.HideActions();
        _view.ShowDownloadAction("Update selected Steam branch");
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
                    "Redownload Version",
                    "Keep Version"
                );
                return;
            }

            _view.ShowConfirmation(
                RedownloadConfirmationMessage,
                ApplyRedownloadAndDownload,
                "Redownload Version",
                "Keep Version"
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
            "Selected branch files removed. Downloading a fresh copy from Steam...",
            LauncherStatusSeverity.Working
        );
        _view.AppendLog("Selected branch files removed before replacement download. Saves and other branches were left in place.");
        _ = DownloadAsync();
    }

    private void ApplyRedownloadBlockedByBranchProblem(string downloadProblem)
    {
        _model.ResetGameFilesForRedownload();
        _refreshGameBranchOptions();
        _view.SetStatus(downloadProblem, LauncherStatusSeverity.Warning);
        _view.AppendLog("Selected branch files removed, but Steam still blocks the replacement download. Saves and other branches were left in place.");
        _view.AppendLog(downloadProblem);
        ShowDownloadReadyAction();
    }
}
