using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void DownloadPressed()
        => _ = DownloadAsync();

    private void RedownloadPressed()
    {
        _view.ShowConfirmation(
            "Redownload game files?\nThis keeps your Steam login but deletes downloaded game files.",
            () =>
            {
                _model.ResetGameFilesForRedownload();
                _view.Actions.HideAll();
                ShowDownloadAction("DOWNLOAD GAME FILES");
                _view.SetStatus("Game files deleted. Download again to rebuild them.");
                _view.AppendLog("Game files were deleted for a clean redownload.");
            }
        );
    }

    private async Task DownloadAsync()
    {
        try
        {
            _view.Download.ShowProgress("Connecting to Steam...");
            await _model.StartDownloadAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Download handler failed: {ex}");
            FailDownload(ex.GetBaseException().Message);
        }
    }

    private void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
    {
        _view.Download.SetProgress(
            progress.Percentage,
            $"{FormatDownloadSize(progress.DownloadedBytes)} / {FormatDownloadSize(progress.TotalBytes)} ({progress.Percentage:F1}%)"
        );
        _view.AppendLog(progress.CurrentFile);
    }

    private static string FormatDownloadSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private void CompleteDownload()
    {
        _view.SetStatus("Download complete! Start game when ready.");
        _view.Download.Visible = false;
        if (LauncherGameFiles.Ready())
        {
            ShowLaunchActions(showUpdate: false);
        }
        else
        {
            _view.Actions.ShowRetry();
        }
    }

    private void FailDownload(string message)
    {
        if (message == null)
        {
            _view.Download.Reset();
            return;
        }

        _view.SetStatus($"Download failed: {message}");
        _view.Download.Reset("RETRY DOWNLOAD");
    }

    private void CancelDownload()
    {
        _view.SetStatus("Download cancelled");
        _view.Download.SetButtonDisabled(false);
    }

    private void ShowDownloadAction(string buttonText)
    {
        _view.Download.Visible = true;
        _view.Download.Reset(buttonText);
    }
}
