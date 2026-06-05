using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private const string NotConnectedMessage = "Not connected";

    private readonly struct DepotConnectionAction
    {
        private DepotConnectionAction(
            Action<string> raiseFailure,
            Func<SteamConnection, Task> run
        )
        {
            RaiseFailure = raiseFailure;
            Run = run;
        }

        private Action<string> RaiseFailure { get; }
        private Func<SteamConnection, Task> Run { get; }

        internal static DepotConnectionAction Download(LauncherModel model)
            => new(
                model.RaiseDownloadFailed,
                connection =>
                {
                    model.BeginDownload(connection);
                    return model.RunDownloadAsync();
                }
            );

        internal static DepotConnectionAction UpdateCheck(LauncherModel model)
            => new(
                model.RaiseUpdateCheckFailed,
                model.CheckForUpdatesWithConnectionAsync
            );

        internal async Task RunAsync(SteamConnection connection)
            => await Run(connection).ConfigureAwait(false);

        internal void FailNotConnected()
            => RaiseFailure(NotConnectedMessage);
    }

    private readonly struct DownloadRunGuard
    {
        private DownloadRunGuard(LauncherModel model, bool acquired)
        {
            Model = model;
            Acquired = acquired;
        }

        private LauncherModel Model { get; }
        internal bool Acquired { get; }

        internal static DownloadRunGuard TryAcquire(LauncherModel model)
            => new(
                model,
                Interlocked.Exchange(ref model._downloadRunning, 1) == 0
            );

        internal void Release()
        {
            if (Acquired)
                Interlocked.Exchange(ref Model._downloadRunning, 0);
        }
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
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            RaiseDownloadFailed("Download already running");
            return;
        }

        try
        {
            await RunWithDepotConnectionAsync(
                DepotConnectionAction.Download(this)
            );
        }
        finally
        {
            run.Release();
        }
    }

    internal Task CheckForUpdatesAsync()
        => RunWithDepotConnectionAsync(
            DepotConnectionAction.UpdateCheck(this)
        );

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

    private async Task RunWithDepotConnectionAsync(
        DepotConnectionAction action
    )
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            action.FailNotConnected();
            return;
        }

        await action.RunAsync(connection).ConfigureAwait(false);
    }

    private bool DownloadIsRunning => Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        await EnsureConnectedAsync();
        return IsLoggedIn && _steamSession.TryGetConnection(out var connection)
            ? connection
            : null;
    }
}
