using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DownloadPressed()
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
            _view.SetStatus(downloadProblem);
            _view.AppendLog(downloadProblem);
            ShowDownloadReadyAction();
            return;
        }

        _ = DownloadAsync();
    }

    private void RedownloadPressed()
        => _view.ShowConfirmation(
            RedownloadConfirmationMessage,
            ApplyRedownload,
            "Redownload Version",
            "Keep Files"
        );

    private void ClearCachedVersionsPressed()
        => _view.ShowConfirmation(
            "Clear inactive cached game versions?\nThis keeps the selected version and removes other downloaded branch caches.",
            ClearCachedVersions,
            "Clear Cache",
            "Keep Cache"
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
        var message = $"Removed {removed} inactive cached game version(s) and {removedRuntimePacks} runtime pack cache(s). Selected version preserved: {selectedVersion}.";
        _view.SetStatus(message);
        _view.AppendLog(message);
    }

    private void ApplyRedownload()
    {
        _model.ResetGameFilesForRedownload();
        DownloadViewUpdate.RedownloadApplied().Apply(this);
    }

    private void ApplyRedownloadAndDownload()
    {
        _model.ResetGameFilesForRedownload();
        _view.SetStatus("Selected game version metadata cache cleared. Rebuilding selected version from Steam...");
        _view.AppendLog("Selected game version metadata cache cleared before replacement download.");
        _ = DownloadAsync();
    }

    private void ApplyRedownloadBlockedByBranchProblem(string downloadProblem)
    {
        _model.ResetGameFilesForRedownload();
        _view.SetStatus(downloadProblem);
        _view.AppendLog("Selected game version cache cleared, but replacement download remains blocked by Steam branch availability evidence.");
        _view.AppendLog(downloadProblem);
        ShowDownloadReadyAction();
    }
}
