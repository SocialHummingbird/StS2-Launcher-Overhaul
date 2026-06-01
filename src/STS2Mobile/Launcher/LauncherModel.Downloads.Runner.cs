using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private async Task RunDownloadAsync()
    {
        try
        {
            await _downloader.DownloadAsync(_downloadCts.Token).ConfigureAwait(false);
            RaiseDownloadCompleted();
        }
        catch (OperationCanceledException)
        {
            RaiseDownloadCancelled();
        }
        catch (Exception ex)
        {
            RaiseDownloadFailed(ex.Message);
            PatchHelper.Log($"[Launcher] Download error: {ex}");
        }
        finally
        {
            ResetDownload();
        }
    }

    private void BeginDownload(SteamConnection connection)
    {
        ResetDownload();
        _downloader = CreateDownloader(connection);
        _downloader.ProgressChanged += RaiseDownloadProgressChanged;
        _downloadCts = new CancellationTokenSource();
    }

    private DepotDownloader CreateDownloader(SteamConnection connection)
    {
        var downloader = new DepotDownloader(connection, _dataDir);
        downloader.LogMessage += RaiseDownloadLogReceived;
        return downloader;
    }

    private void CancelDownloadForRetry()
    {
        CancelDownload();

        if (DownloadIsRunning)
            PatchHelper.Log(
                "[Launcher] Retry requested while download is active; cancellation requested"
            );
        else
            ResetDownload();
    }

    private void CancelDownload()
    {
        try
        {
            _downloadCts?.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    private void ResetDownload()
    {
        _downloader?.Dispose();
        _downloadCts?.Dispose();
        _downloader = null;
        _downloadCts = null;
    }
}
