using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string RedownloadConfirmationMessage =
        "Redownload game files?\nThis keeps your Steam login but deletes downloaded game files.";
    private const string DownloadGameFilesButtonText = "DOWNLOAD GAME FILES";
    private const string DownloadCompleteStatus =
        "Download complete! Start game when ready.";
    private const string DownloadCancelledStatus = "Download cancelled";
    private const string RetryDownloadButtonText = "RETRY DOWNLOAD";
    private const string RedownloadStatusMessage =
        "Game files deleted. Download again to rebuild them.";
    private const string RedownloadLogMessage =
        "Game files were deleted for a clean redownload.";

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

        internal static DownloadViewUpdate Ready()
            => new(
                downloadAction: DownloadGameFilesButtonText,
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

        internal static DownloadViewUpdate Completed(bool filesReady)
            => new(
                status: DownloadCompleteStatus,
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
                    status: $"Download failed: {message}",
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
        => _ = DownloadAsync();

    private void RedownloadPressed()
        => _view.ShowConfirmation(RedownloadConfirmationMessage, ApplyRedownload);

    private void ApplyRedownload()
    {
        _model.ResetGameFilesForRedownload();
        DownloadViewUpdate.RedownloadApplied().Apply(this);
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
        => DownloadViewUpdate.Completed(LauncherGameFiles.Ready()).Apply(this);

    private void FailDownload(string message)
        => DownloadViewUpdate.Failed(message).Apply(this);

    private void CancelDownload()
        => DownloadViewUpdate.Cancelled().Apply(this);

    private void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(this);
}
