using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string RedownloadConfirmationMessage =
        "Redownload selected game version?\nThis keeps your Steam login and other cached versions, but deletes the selected version's downloaded files.";
    private const string DownloadGameFilesButtonText = "DOWNLOAD SELECTED VERSION";
    private const string RedownloadSelectedVersionButtonText = "REDOWNLOAD SELECTED VERSION";
    private const string DownloadCancelledStatus = "Download cancelled";
    private const string RetryDownloadButtonText = "RETRY DOWNLOAD";
    private const string RedownloadStatusMessage =
        "Selected game version deleted. Download again to rebuild it.";
    private const string RedownloadLogMessage =
        "Selected game version files were deleted for a clean redownload.";
    private const string BlockedRedownloadConfirmationMessage =
        "Delete selected game version cache?\nThis branch is currently blocked by Steam app-info availability evidence, so the launcher will delete the selected local cache but will not start a replacement download.";

    private readonly struct DownloadViewUpdate
    {
        private DownloadViewUpdate(
            string status = null,
            string logMessage = null,
            string downloadAction = null,
            string resetDownloadButton = null,
            LaunchUpdateAction? launchAction = null,
            bool? downloadButtonDisabled = null,
            bool hideActions = false,
            bool hideDownload = false,
            bool showRetry = false,
            bool resetDownload = false
        )
        {
            Status = status;
            LogMessage = logMessage;
            DownloadAction = downloadAction;
            ResetDownloadButton = resetDownloadButton;
            LaunchAction = launchAction;
            DownloadButtonDisabled = downloadButtonDisabled;
            HideActions = hideActions;
            HideDownload = hideDownload;
            ShowRetry = showRetry;
            ResetDownload = resetDownload;
        }

        private string Status { get; }
        private string LogMessage { get; }
        private string DownloadAction { get; }
        private string ResetDownloadButton { get; }
        private LaunchUpdateAction? LaunchAction { get; }
        private bool? DownloadButtonDisabled { get; }
        private bool HideActions { get; }
        private bool HideDownload { get; }
        private bool ShowRetry { get; }
        private bool ResetDownload { get; }

        internal static DownloadViewUpdate Ready(string buttonText = DownloadGameFilesButtonText)
            => new(
                downloadAction: buttonText,
                downloadButtonDisabled: false
            );

        internal static DownloadViewUpdate RedownloadApplied()
            => new(
                status: RedownloadStatusMessage,
                logMessage: RedownloadLogMessage,
                downloadAction: DownloadGameFilesButtonText,
                downloadButtonDisabled: false,
                hideActions: true
            );

        internal static DownloadViewUpdate Completed(bool filesReady, string readinessProblem)
            => new(
                status: filesReady
                    ? $"Selected game version downloaded ({SelectedGameVersionName()}). Start game when ready."
                    : readinessProblem,
                hideDownload: true,
                launchAction: filesReady
                    ? LaunchUpdateAction.Hidden
                    : (LaunchUpdateAction?)null,
                showRetry: !filesReady
            );

        internal static DownloadViewUpdate Failed(string message)
            => string.IsNullOrEmpty(message)
                ? new(resetDownload: true)
                : new(
                    status: $"Download failed for selected game version ({SelectedGameVersionName()}): {message}",
                    resetDownloadButton: RetryDownloadButtonText
                );

        internal static DownloadViewUpdate Cancelled()
            => new(
                status: DownloadCancelledStatus,
                downloadButtonDisabled: false
            );

        internal void Apply(LauncherController controller)
        {
            var view = controller._view;

            if (HideActions)
                view.HideActions();

            if (DownloadAction != null)
                view.ShowDownloadAction(DownloadAction);

            if (DownloadButtonDisabled.HasValue)
                view.SetDownloadButtonDisabled(DownloadButtonDisabled.Value);

            if (Status != null)
                view.SetStatus(Status);

            if (LogMessage != null)
                view.AppendLog(LogMessage);

            if (HideDownload)
                view.HideDownload();

            if (ResetDownload)
                view.ResetDownload();
            else if (ResetDownloadButton != null)
                view.ResetDownload(ResetDownloadButton);

            if (LaunchAction.HasValue)
                controller.ShowLaunchActions(LaunchAction.Value);

            if (ShowRetry)
                view.ShowRetry();
        }
    }

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
                    "DELETE CACHE",
                    "KEEP CACHE"
                );
                return;
            }

            _view.ShowConfirmation(
                RedownloadConfirmationMessage,
                ApplyRedownloadAndDownload,
                "REDOWNLOAD VERSION",
                "KEEP FILES"
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
            "REDOWNLOAD VERSION",
            "KEEP FILES"
        );

    private void ClearCachedVersionsPressed()
        => _view.ShowConfirmation(
            "Clear inactive cached game versions?\nThis keeps the selected version and removes other downloaded branch caches.",
            ClearCachedVersions,
            "CLEAR CACHE",
            "KEEP CACHE"
        );

    private void ClearCachedVersions()
    {
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var selectedVersion = STS2Mobile.Steam.SteamGameBranch.DisplayName(selectedBranch);
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

    private async Task DownloadAsync()
    {
        try
        {
            _view.ShowDownloadProgress("Connecting to Steam...");
            await _model.StartDownloadAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Download handler failed: {ex}");
            FailDownload(ex.GetBaseException().Message);
        }
    }

    private void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
        => progress.ApplyTo(_view.SetDownloadProgress, _view.AppendLog);

    private void CompleteDownload()
    {
        RefreshGameBranchOptions();
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_model.DataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_model.DataDir, branch);
        var filesReady = LauncherGameFiles.Ready(_model.DataDir, branch);
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        LauncherRuntimeSlotEvidence.Write(_model.DataDir, branch, filesReady, readinessProblem);
        DownloadViewUpdate.Completed(
            filesReady,
            readinessProblem
        ).Apply(this);
        var integritySummary = LauncherGameFiles.BranchIntegritySummary(_model.DataDir, branch);
        if (!string.IsNullOrWhiteSpace(integritySummary))
            _view.AppendLog(integritySummary);
    }

    private void FailDownload(string message)
    {
        RefreshGameBranchOptions();
        DownloadViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message)
        ).Apply(this);
    }

    private void CancelDownload()
        => DownloadViewUpdate.Cancelled().Apply(this);

    private void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(this);

    private void ShowRedownloadSelectedVersionAction()
        => DownloadViewUpdate.Ready(RedownloadSelectedVersionButtonText).Apply(this);
}
