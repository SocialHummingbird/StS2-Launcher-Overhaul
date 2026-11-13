using System;
using System.Threading;
using STS2Mobile.Steam;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;
internal partial class LauncherModel
{
    private readonly LauncherDownloadLifetime _downloadLifetime = new();
    private int _downloadGeneration;
    internal event Action<DepotDownloader.DownloadProgress> DownloadProgressChanged;
    internal event Action<string> DownloadLogReceived;
    internal event Action<BranchInstallCompletion> DownloadCompleted;
    internal event Action<LauncherBranchOperationFailure> DownloadFailed;
    internal event Action<string> DownloadCancelled;
    internal event Action<LauncherUpdateCheckResult> UpdateCheckCompleted;
    internal event Action<LauncherBranchOperationFailure> UpdateCheckFailed;
    internal event Action BranchCatalogRefreshCompleted;
    internal event Action<string> BranchCatalogRefreshFailed;
    internal event Action<string> WorkshopSyncLogReceived;
    internal event Action<string> WorkshopSyncCompleted;
    internal event Action<string> WorkshopSyncFailed;
    internal event Action<int> WorkshopClearCompleted;
    internal event Action<string> WorkshopClearFailed;
    private bool DownloadIsRunning => _downloadLifetime.IsRunning;
    internal bool SelectedVersionOperationIsRunning => DownloadIsRunning;
    internal bool CanPauseDownload => _downloadLifetime.CanPause;
    internal Task PauseDownloadAsync()
    {
        lock (_downloadSync) return _downloadLifetime.PauseAsync();
    }

    internal Action BindCurrentDownloadCallback(Action callback)
    {
        lock (_downloadSync)
        {
            var generation = _downloadGeneration;
            return () => RaiseCurrentDownload(generation, callback);
        }
    }

    private void RaiseCurrentDownload(int generation, Action callback)
    {
        lock (_downloadSync)
        {
            if (generation == _downloadGeneration && _disposeTask == null)
                callback();
        }
    }

    private const string NotConnectedMessage = "Not connected";
    private readonly struct DepotConnectionAction
    {
        private DepotConnectionAction(Action<string> raiseFailure, Func<SteamConnection, Task> run)
        {
            RaiseFailure = raiseFailure;
            Run = run;
        }

        private Action<string> RaiseFailure { get; }
        private Func<SteamConnection, Task> Run { get; }

        internal static DepotConnectionAction UpdateCheck(LauncherModel model, string branch) => new(message => model.RaiseUpdateCheckFailed(branch, message), connection => model.CheckForUpdatesWithConnectionAsync(connection, branch));
        internal static DepotConnectionAction BranchCatalogRefresh(LauncherModel model) => new(model.RaiseBranchCatalogRefreshFailed, model.RefreshBranchCatalogWithConnectionAsync);
        internal static DepotConnectionAction WorkshopSync(LauncherModel model) => new(model.RaiseWorkshopSyncFailed, model.SyncWorkshopWithConnectionAsync);
        internal async Task RunAsync(SteamConnection connection) => await Run(connection).ConfigureAwait(false);
        internal void FailNotConnected() => RaiseFailure(NotConnectedMessage);
    }

    private async Task CheckForUpdatesWithConnectionAsync(SteamConnection connection, string branch)
    {
        try
        {
            using var downloader = CreateDownloader(connection, branch);
            bool hasUpdate = await downloader.CheckForUpdatesAsync().ConfigureAwait(false);
            RaiseUpdateCheckCompleted(branch, hasUpdate);
        }
        catch (Exception ex)
        {
            RaiseUpdateCheckFailed(branch, ex.Message);
        }
    }

    private async Task RefreshBranchCatalogWithConnectionAsync(SteamConnection connection)
    {
        try
        {
            using var downloader = CreateDownloader(connection);
            await downloader.RefreshBranchCatalogAsync().ConfigureAwait(false);
            RaiseBranchCatalogRefreshCompleted();
        }
        catch (Exception ex)
        {
            RaiseBranchCatalogRefreshFailed(ex.Message);
        }
    }

    private async Task RunWithDepotConnectionAsync(DepotConnectionAction action)
    {
        LauncherLaunchMarkers.RecordPhase("steam depot connection requested");
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            LauncherLaunchMarkers.RecordPhase("steam depot connection unavailable");
            action.FailNotConnected();
            return;
        }

        LauncherLaunchMarkers.RecordPhase("steam depot connection ready");
        await action.RunAsync(connection).ConfigureAwait(false);
    }

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        LauncherLaunchMarkers.RecordPhase("steam ensure connected");
        await EnsureConnectedAsync();
        return IsLoggedIn && _steamSession.TryGetConnection(out var connection) ? connection : null;
    }

    private void RaiseDownloadCompleted(BranchInstallCompletion completion) => Raise(DownloadCompleted, completion, nameof(DownloadCompleted));
    private void RaiseDownloadCancelled(string branch) => Raise(DownloadCancelled, branch, nameof(DownloadCancelled));
    private void RaiseDownloadFailed(string branch, string message) => Raise(DownloadFailed, new LauncherBranchOperationFailure(branch, message), nameof(DownloadFailed));
    private void RaiseDownloadLogReceived(string message) => Raise(DownloadLogReceived, message, nameof(DownloadLogReceived));
    private void RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress progress) => Raise(DownloadProgressChanged, progress, nameof(DownloadProgressChanged));
    private void RaiseUpdateCheckCompleted(string branch, bool hasUpdate) => Raise(UpdateCheckCompleted, new LauncherUpdateCheckResult(branch, hasUpdate), nameof(UpdateCheckCompleted));
    private void RaiseUpdateCheckFailed(string branch, string message) => Raise(UpdateCheckFailed, new LauncherBranchOperationFailure(branch, message), nameof(UpdateCheckFailed));
    private void RaiseBranchCatalogRefreshCompleted() => Raise(BranchCatalogRefreshCompleted, nameof(BranchCatalogRefreshCompleted));
    private void RaiseBranchCatalogRefreshFailed(string message) => Raise(BranchCatalogRefreshFailed, message, nameof(BranchCatalogRefreshFailed));
    private void RaiseWorkshopSyncLogReceived(string message) => Raise(WorkshopSyncLogReceived, message, nameof(WorkshopSyncLogReceived));
    private void RaiseWorkshopSyncCompleted(string summary) => Raise(WorkshopSyncCompleted, summary, nameof(WorkshopSyncCompleted));
    private void RaiseWorkshopSyncFailed(string message) => Raise(WorkshopSyncFailed, message, nameof(WorkshopSyncFailed));
    private void RaiseWorkshopClearCompleted(int removedCount) => Raise(WorkshopClearCompleted, removedCount, nameof(WorkshopClearCompleted));
    private void RaiseWorkshopClearFailed(string message) => Raise(WorkshopClearFailed, message, nameof(WorkshopClearFailed));
    private async Task<BranchInstallCompletion> RunDownloadAsync(SteamConnection connection, string branch, CancellationToken token, int generation)
    {
        using var downloader = CreateDownloader(connection, branch);
        downloader.ProgressChanged += progress => RaiseCurrentDownload(generation, () => RaiseDownloadProgressChanged(progress));
        var completion = await downloader.DownloadAsync(token).ConfigureAwait(false);
        if (LocalPckRepairOperation.ClassifyForLauncherRouting(_dataDir, branch) == InstalledGameVersionReadiness.AutomaticRepair)
        {
            _downloadLifetime.EnterLocalRepair();
            completion = await RunAutomaticPckRepairCoreAsync(branch, generation).ConfigureAwait(false);
        }

        return completion;
    }

    private async Task<BranchInstallCompletion> RunAutomaticPckRepairCoreAsync(string branch, int generation)
    {
        RaiseCurrentDownload(generation, () =>
        {
            RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress.Status(LocalPckRepairOperation.ProgressMessage));
            RaiseDownloadLogReceived(LocalPckRepairOperation.ProgressMessage);
        });
        return await Task.Run(() => LocalPckRepairOperation.RepairAutomatic(_dataDir, branch)).ConfigureAwait(false);
    }

    private DepotDownloader CreateDownloader(SteamConnection connection) => CreateDownloader(connection, LauncherPreferences.ReadGameBranch());
    private DepotDownloader CreateDownloader(SteamConnection connection, string branch)
    {
        var downloader = new DepotDownloader(connection, _dataDir, branch);
        downloader.LogMessage += RaiseDownloadLogReceived;
        return downloader;
    }

    internal Task StartDownloadAsync(string branch) => StartSelectedVersionOperationAsync(branch, automaticRepairOnly: false);
    internal Task StartAutomaticRepairAsync(string branch) => StartSelectedVersionOperationAsync(branch, automaticRepairOnly: true);
    private Task StartSelectedVersionOperationAsync(string branch, bool automaticRepairOnly)
    {
        lock (_downloadSync)
        {
            branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
            automaticRepairOnly |= LocalPckRepairOperation.ClassifyForLauncherRouting(_dataDir, branch) == InstalledGameVersionReadiness.AutomaticRepair;
            LauncherLaunchMarkers.RecordPhase(automaticRepairOnly ? "automatic PCK repair model start" : "download model start", $"branch={branch}; localRepairOnly={automaticRepairOnly}");
            BranchInstallCompletion completion = null;
            var generation = 0;
            var operationToken = CancellationToken.None;
            var operation = _downloadLifetime.TryRunAsync(async token =>
            {
                operationToken = token;
                generation = Interlocked.Increment(ref _downloadGeneration);
                LauncherLaunchReadinessCache.Clear(automaticRepairOnly ? "automatic PCK repair model started" : "download model started");
                if (automaticRepairOnly)
                {
                    completion = await RunAutomaticPckRepairCoreAsync(branch, generation).ConfigureAwait(false);
                    return;
                }

                token.ThrowIfCancellationRequested();
                var connection = await GetDepotConnectionAsync().ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (connection == null)
                    throw new InvalidOperationException(NotConnectedMessage);
                completion = await RunDownloadAsync(connection, branch, token, generation).ConfigureAwait(false);
            }, pausable: !automaticRepairOnly);
            if (operation == null)
            {
                LauncherLaunchMarkers.RecordPhase(automaticRepairOnly ? "automatic PCK repair model blocked" : "download model blocked", "Selected-version operation already running");
                RaiseDownloadLogReceived("Selected-version operation already running");
                return Task.CompletedTask;
            }

            return CompleteSelectedVersionOperationAsync(operation, branch, generation, operationToken, () => completion);
        }
    }

    private async Task CompleteSelectedVersionOperationAsync(Task operation, string branch, int generation, CancellationToken token, Func<BranchInstallCompletion> completion)
    {
        try
        {
            await operation.ConfigureAwait(false);
            RaiseCurrentDownload(generation, () => RaiseDownloadCompleted(completion()));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            RaiseCurrentDownload(generation, () => RaiseDownloadCancelled(branch));
        }
        catch (Exception error)
        {
            RaiseCurrentDownload(generation, () => RaiseDownloadFailed(branch, error.Message));
            PatchHelper.Log($"[Launcher] Selected-version operation failed: {error}");
        }
        finally
        {
            LauncherLaunchMarkers.RecordPhase("selected-version operation model finished");
        }
    }

    internal Task CheckForUpdatesAsync(string branch)
    {
        branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
        LauncherLaunchMarkers.RecordPhase("update check model start", $"branch={branch}");
        return RunWithDepotConnectionAsync(DepotConnectionAction.UpdateCheck(this, branch));
    }

    internal Task RefreshBranchCatalogAsync()
    {
        LauncherLaunchMarkers.RecordPhase("branch catalog refresh model start");
        return RunWithDepotConnectionAsync(DepotConnectionAction.BranchCatalogRefresh(this));
    }
}
