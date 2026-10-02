using System;
using System.Threading;
using STS2Mobile.Steam;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;
internal partial class LauncherModel
{
    private CancellationTokenSource _downloadCts;
    private DepotDownloader _downloader;
    private int _downloadRunning;
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
    private bool DownloadIsRunning => Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;
    internal bool SelectedVersionOperationIsRunning => DownloadIsRunning;

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

        internal static DepotConnectionAction Download(LauncherModel model, string branch) => new(message => model.RaiseDownloadFailed(branch, message), connection =>
        {
            model.BeginDownload(connection, branch);
            return model.RunDownloadAsync(branch);
        });
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
    private readonly struct DownloadRunGuard
    {
        private DownloadRunGuard(LauncherModel model, bool acquired)
        {
            Model = model;
            Acquired = acquired;
        }

        private LauncherModel Model { get; }
        internal bool Acquired { get; }

        internal static DownloadRunGuard TryAcquire(LauncherModel model) => new(model, Interlocked.Exchange(ref model._downloadRunning, 1) == 0);
        internal void Release()
        {
            if (Acquired)
                Interlocked.Exchange(ref Model._downloadRunning, 0);
        }
    }

    private async Task RunDownloadAsync(string branch)
    {
        try
        {
            var completion = await _downloader.DownloadAsync(_downloadCts.Token).ConfigureAwait(false);
            if (LocalPckRepairOperation.ClassifyForLauncherRouting(_dataDir, branch) == InstalledGameVersionReadiness.AutomaticRepair)
            {
                completion = await RunAutomaticPckRepairCoreAsync(branch).ConfigureAwait(false);
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
            var completion = await RunAutomaticPckRepairCoreAsync(branch).ConfigureAwait(false);
            RaiseDownloadCompleted(completion);
        }
        catch (Exception ex)
        {
            RaiseDownloadFailed(branch, ex.Message);
            PatchHelper.Log($"[Launcher] Automatic PCK repair error: {ex}");
        }
    }

    private async Task<BranchInstallCompletion> RunAutomaticPckRepairCoreAsync(string branch)
    {
        RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress.Status(LocalPckRepairOperation.ProgressMessage));
        RaiseDownloadLogReceived(LocalPckRepairOperation.ProgressMessage);
        return await Task.Run(() => LocalPckRepairOperation.RepairAutomatic(_dataDir, branch)).ConfigureAwait(false);
    }

    private void BeginDownload(SteamConnection connection, string branch)
    {
        ResetDownload();
        _downloader = CreateDownloader(connection, branch);
        _downloader.ProgressChanged += RaiseDownloadProgressChanged;
        _downloadCts = new CancellationTokenSource();
    }

    private DepotDownloader CreateDownloader(SteamConnection connection) => CreateDownloader(connection, LauncherPreferences.ReadGameBranch());
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
            PatchHelper.Log("[Launcher] Retry requested while download is active; cancellation requested");
        else
            ResetDownload();
    }

    private void CancelDownload()
    {
        try
        {
            _downloadCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void ResetDownload()
    {
        _downloader?.Dispose();
        _downloadCts?.Dispose();
        _downloader = null;
        _downloadCts = null;
    }

    internal Task StartDownloadAsync(string branch) => StartSelectedVersionOperationAsync(branch, automaticRepairOnly: false);
    internal Task StartAutomaticRepairAsync(string branch) => StartSelectedVersionOperationAsync(branch, automaticRepairOnly: true);
    private async Task StartSelectedVersionOperationAsync(string branch, bool automaticRepairOnly)
    {
        branch = STS2Mobile.Steam.SteamGameBranch.Normalize(branch);
        LauncherLaunchMarkers.RecordPhase(automaticRepairOnly ? "automatic PCK repair model start" : "download model start", $"branch={branch}; localRepairOnly={automaticRepairOnly}");
        var run = DownloadRunGuard.TryAcquire(this);
        if (!run.Acquired)
        {
            LauncherLaunchMarkers.RecordPhase(automaticRepairOnly ? "automatic PCK repair model blocked" : "download model blocked", "Selected-version operation already running");
            RaiseDownloadFailed(branch, "Selected-version operation already running");
            return;
        }

        try
        {
            LauncherLaunchReadinessCache.Clear(automaticRepairOnly ? "automatic PCK repair model started" : "download model started");
            if (automaticRepairOnly)
            {
                await RunAutomaticPckRepairAsync(branch);
                return;
            }

            if (LocalPckRepairOperation.ClassifyForLauncherRouting(_dataDir, branch) == InstalledGameVersionReadiness.AutomaticRepair)
            {
                await RunAutomaticPckRepairAsync(branch);
                return;
            }

            await RunWithDepotConnectionAsync(DepotConnectionAction.Download(this, branch));
        }
        finally
        {
            LauncherLaunchMarkers.RecordPhase(automaticRepairOnly ? "automatic PCK repair model finished" : "download model finished");
            run.Release();
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
