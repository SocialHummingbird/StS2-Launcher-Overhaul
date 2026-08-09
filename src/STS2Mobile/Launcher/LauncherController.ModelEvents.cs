using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireModelEvents()
    {
        _model.SessionStateChanged += OnMainThread<LauncherModel.SessionState>(OnSessionStateChanged);
        _model.LogReceived += OnMainThread<string>(_view.AppendLog);
        _model.CodeNeeded += OnMainThread<bool>(_session.ShowCodePrompt);
        _model.DownloadProgressChanged += OnMainThread<DepotDownloader.DownloadProgress>(
            _downloads.UpdateDownloadProgress
        );
        _model.DownloadLogReceived += OnMainThread<string>(_view.AppendLog);
        _model.DownloadCompleted += OnMainThread<string>(OnDownloadCompleted);
        _model.DownloadFailed += OnMainThread<LauncherBranchOperationFailure>(OnDownloadFailed);
        _model.DownloadCancelled += OnMainThread<string>(OnDownloadCancelled);
        _model.UpdateCheckCompleted += OnMainThread<LauncherUpdateCheckResult>(_versions.CompleteUpdateCheck);
        _model.UpdateCheckFailed += OnMainThread<LauncherBranchOperationFailure>(_versions.FailUpdateCheck);
        _model.BranchCatalogRefreshCompleted += OnMainThread(_versions.CompleteBranchCatalogRefresh);
        _model.BranchCatalogRefreshFailed += OnMainThread<string>(_versions.FailBranchCatalogRefresh);
        _model.WorkshopSyncLogReceived += OnMainThread<string>(_view.AppendLog);
        _model.WorkshopSyncCompleted += OnMainThread<string>(_workshop.CompleteSync);
        _model.WorkshopSyncFailed += OnMainThread<string>(_workshop.FailSync);
        _model.WorkshopClearCompleted += OnMainThread<int>(_workshop.CompleteClear);
        _model.WorkshopClearFailed += OnMainThread<string>(_workshop.FailClear);
    }

    private void OnSessionStateChanged(LauncherModel.SessionState state)
    {
        _session.UpdateUI(state);
        RefreshSaveSyncPresentation();
        if (state == LauncherModel.SessionState.LoggedIn)
            StartAutomaticSaveSync();
    }

    private void OnDownloadCompleted(string branch)
    {
        _downloads.CompleteDownload(branch);
        _session.RefreshHomeGameState();
    }

    private void OnDownloadFailed(LauncherBranchOperationFailure failure)
    {
        _downloads.FailDownload(failure);
        _session.RefreshHomeGameState();
    }

    private void OnDownloadCancelled(string branch)
    {
        _downloads.CancelDownload(branch);
        _session.RefreshHomeGameState();
    }

    private Action OnMainThread(Action action)
        => () => _runOnMainThread(action);

    private Action<T> OnMainThread<T>(Action<T> action)
        => value => _runOnMainThread(() => action(value));

}
