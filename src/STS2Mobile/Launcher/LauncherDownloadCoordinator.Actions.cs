using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

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
        if (_model.SelectedVersionOperationIsRunning)
            return;
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
        => _ = ApplyRedownloadAsync(downloadAfterReset: false);

    private void ApplyRedownloadAndDownload()
        => _ = ApplyRedownloadAsync(downloadAfterReset: true);

    private void ApplyRedownloadBlockedByBranchProblem(string downloadProblem)
        => _ = ApplyRedownloadAsync(downloadAfterReset: false, downloadProblem);

    private async Task ApplyRedownloadAsync(bool downloadAfterReset, string downloadProblem = null)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        _automaticRepairContinuation = null;
        _view.HideActions();
        _view.ShowDownloadProgress("Stopping file operations and removing the selected version…");
        _view.SetDownloadPauseAvailable(false);
        try
        {
            await _model.ResetGameFilesForRedownloadAsync(branch);
            _refreshGameBranchOptions();
            if (!string.IsNullOrWhiteSpace(downloadProblem))
            {
                _view.SetStatus(downloadProblem, LauncherStatusSeverity.Warning);
                _view.AppendLog("Selected branch files removed, but Steam still blocks the replacement download. Saves and other branches were left in place.");
                _view.AppendLog(downloadProblem);
                ShowDownloadReadyAction();
            }
            else if (downloadAfterReset)
            {
                _view.SetStatus("Selected branch files removed. Downloading a fresh copy from Steam...", LauncherStatusSeverity.Working);
                _view.AppendLog("Selected branch files removed before replacement download. Saves and other branches were left in place.");
                await RunSelectedVersionOperationAsync(branch, automaticRepairOnly: false);
            }
            else
            {
                DownloadViewUpdate.RedownloadApplied().Apply(_view, _launch);
            }
        }
        catch (Exception error)
        {
            PatchHelper.Log($"[Launcher] Redownload reset failed: {error}");
            FailDownload(new LauncherBranchOperationFailure(branch, error.GetBaseException().Message));
        }
    }
}
