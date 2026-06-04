using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private readonly struct DepotConnectionOperation
    {
        private readonly string? _missingConnectionMessage;
        private readonly Action<string> _raiseFailure;
        private readonly Func<SteamConnection, Task> _run;

        internal DepotConnectionOperation(
            string? missingConnectionMessage,
            Action<string> raiseFailure,
            Func<SteamConnection, Task> run
        )
        {
            _missingConnectionMessage = missingConnectionMessage;
            _raiseFailure = raiseFailure;
            _run = run;
        }

        internal void FailMissingConnection()
            => _raiseFailure(_missingConnectionMessage);

        internal Task RunAsync(SteamConnection connection)
            => _run(connection);
    }

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
            await RunWithDepotConnectionAsync(new DepotConnectionOperation(
                null,
                RaiseDownloadFailed,
                connection =>
                {
                    BeginDownload(connection);
                    return RunDownloadAsync();
                }
            ));
        }
        finally
        {
            Interlocked.Exchange(ref _downloadRunning, 0);
        }
    }

    internal Task CheckForUpdatesAsync()
        => RunWithDepotConnectionAsync(new DepotConnectionOperation(
            "Not connected",
            RaiseUpdateCheckFailed,
            CheckForUpdatesWithConnectionAsync
        ));

    private async Task CheckForUpdatesWithConnectionAsync(SteamConnection connection)
    {
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

    private async Task RunWithDepotConnectionAsync(DepotConnectionOperation operation)
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            operation.FailMissingConnection();
            return;
        }

        await operation.RunAsync(connection).ConfigureAwait(false);
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
