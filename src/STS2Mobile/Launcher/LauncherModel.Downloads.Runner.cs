using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private async Task RunDownloadAsync(string branch)
    {
        try
        {
            var completion = await _downloader.DownloadAsync(_downloadCts.Token)
                .ConfigureAwait(false);
            if (LocalPckRepairOperation.ClassifyForLauncherRouting(
                    _dataDir,
                    branch
                ) == InstalledGameVersionReadiness.AutomaticRepair)
            {
                completion = await RunAutomaticPckRepairCoreAsync(branch)
                    .ConfigureAwait(false);
            }
            RaiseDownloadCompleted(completion);
        }
        catch (OperationCanceledException)
        {
            RaiseDownloadCancelled(branch);
        }
        catch (Exception ex)
        {
            RaiseDownloadFailed(branch, ex.Message);
            PatchHelper.Log($"[Launcher] Download error: {ex}");
        }
        finally
        {
            ResetDownload();
        }
    }

    private async Task RunAutomaticPckRepairAsync(string branch)
    {
        try
        {
            var completion = await RunAutomaticPckRepairCoreAsync(branch)
                .ConfigureAwait(false);
            RaiseDownloadCompleted(completion);
        }
        catch (Exception ex)
        {
            RaiseDownloadFailed(branch, ex.Message);
            PatchHelper.Log($"[Launcher] Automatic PCK repair error: {ex}");
        }
    }

    private async Task<BranchInstallCompletion> RunAutomaticPckRepairCoreAsync(
        string branch
    )
    {
        RaiseDownloadProgressChanged(
            DepotDownloader.DownloadProgress.Status(
                LocalPckRepairOperation.ProgressMessage
            )
        );
        RaiseDownloadLogReceived(LocalPckRepairOperation.ProgressMessage);
        return await Task.Run(() =>
                LocalPckRepairOperation.RepairAutomatic(_dataDir, branch)
            )
            .ConfigureAwait(false);
    }

    private void BeginDownload(SteamConnection connection, string branch)
    {
        ResetDownload();
        _downloader = CreateDownloader(connection, branch);
        _downloader.ProgressChanged += RaiseDownloadProgressChanged;
        _downloadCts = new CancellationTokenSource();
    }

    private DepotDownloader CreateDownloader(SteamConnection connection)
        => CreateDownloader(connection, LauncherPreferences.ReadGameBranch());

    private DepotDownloader CreateDownloader(SteamConnection connection, string branch)
    {
        var downloader = new DepotDownloader(connection, _dataDir, branch);
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
