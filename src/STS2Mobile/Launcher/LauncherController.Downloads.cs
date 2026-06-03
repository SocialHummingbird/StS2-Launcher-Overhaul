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
    private const string RedownloadStatusMessage =
        "Game files deleted. Download again to rebuild them.";
    private const string RedownloadLogMessage =
        "Game files were deleted for a clean redownload.";

    private void DownloadPressed()
        => _ = DownloadAsync();

    private void RedownloadPressed()
        => _view.ShowConfirmation(RedownloadConfirmationMessage, ApplyRedownload);

    private void ApplyRedownload()
    {
        _model.ResetGameFilesForRedownload();
        _view.HideActions();
        _view.ShowDownloadAction(DownloadGameFilesButtonText);
        _view.SetStatus(RedownloadStatusMessage);
        _view.AppendLog(RedownloadLogMessage);
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
        _view.SetStatus("Download complete! Start game when ready.");
        _view.HideDownload();
        if (LauncherGameFiles.Ready())
        {
            ShowLaunchActions(showUpdate: false);
        }
        else
        {
            _view.ShowRetry();
        }
    }

    private void FailDownload(string message)
    {
        if (message == null)
        {
            _view.ResetDownload();
            return;
        }

        _view.SetStatus($"Download failed: {message}");
        _view.ResetDownload("RETRY DOWNLOAD");
    }

    private void CancelDownload()
    {
        _view.SetStatus("Download cancelled");
        _view.SetDownloadButtonDisabled(false);
    }

    private void ShowDownloadAction(string buttonText)
        => _view.ShowDownloadAction(buttonText);
}
