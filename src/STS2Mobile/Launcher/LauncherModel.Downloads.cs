using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private CancellationTokenSource _downloadCts;
    private DepotDownloader _downloader;
    private int _downloadRunning;

    internal event Action<DepotDownloader.DownloadProgress> DownloadProgressChanged;
    internal event Action<string> DownloadLogReceived;
    internal event Action DownloadCompleted;
    internal event Action<string> DownloadFailed;
    internal event Action DownloadCancelled;
    internal event Action<bool> UpdateCheckCompleted;
    internal event Action<string> UpdateCheckFailed;

    internal async Task StartDownloadAsync()
    {
        if (!TryMarkDownloadRunning())
        {
            RaiseDownloadFailed("Download already running");
            return;
        }

        try
        {
            var connection = await GetDepotConnectionAsync();
            if (connection == null)
            {
                RaiseDownloadFailed(null);
                return;
            }

            BeginDownload(connection);
            await RunDownloadAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _downloadRunning, 0);
        }
    }

    internal async Task CheckForUpdatesAsync()
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            RaiseUpdateCheckFailed("Not connected");
            return;
        }

        try
        {
            using var downloader = CreateDownloader(connection);
            bool hasUpdate = await downloader.CheckForUpdatesAsync().ConfigureAwait(false);
            RaiseUpdateCheckCompleted(hasUpdate);
        }
        catch (Exception ex)
        {
            RaiseUpdateCheckFailed(ex.Message);
        }
    }

    private bool DownloadIsRunning => Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;

    private bool TryMarkDownloadRunning()
        => Interlocked.Exchange(ref _downloadRunning, 1) == 0;

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        await EnsureConnectedAsync();
        return IsLoggedIn && _steamSession.TryGetConnection(out var connection)
            ? connection
            : null;
    }
}
