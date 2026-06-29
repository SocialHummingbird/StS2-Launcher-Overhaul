using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireModelEvents()
    {
        _model.SessionStateChanged += OnMainThread<LauncherModel.SessionState>(_session.UpdateUI);
        _model.LogReceived += OnMainThread<string>(_view.AppendLog);
        PatchHelper.LogEmitted += AppendCloudLog;
        _model.CodeNeeded += OnMainThread<bool>(_session.ShowCodePrompt);
        _model.DownloadProgressChanged += OnMainThread<DepotDownloader.DownloadProgress>(
            _downloads.UpdateDownloadProgress
        );
        _model.DownloadLogReceived += OnMainThread<string>(_view.AppendLog);
        _model.DownloadCompleted += OnMainThread(_downloads.CompleteDownload);
        _model.DownloadFailed += OnMainThread<string>(_downloads.FailDownload);
        _model.DownloadCancelled += OnMainThread(_downloads.CancelDownload);
        _model.UpdateCheckCompleted += OnMainThread<bool>(_versions.CompleteUpdateCheck);
        _model.UpdateCheckFailed += OnMainThread<string>(_versions.FailUpdateCheck);
        _model.BranchCatalogRefreshCompleted += OnMainThread(_versions.CompleteBranchCatalogRefresh);
        _model.BranchCatalogRefreshFailed += OnMainThread<string>(_versions.FailBranchCatalogRefresh);
        _model.WorkshopSyncLogReceived += OnMainThread<string>(_view.AppendLog);
        _model.WorkshopSyncCompleted += OnMainThread<string>(_workshop.CompleteSync);
        _model.WorkshopSyncFailed += OnMainThread<string>(_workshop.FailSync);
        _model.WorkshopClearCompleted += OnMainThread<int>(_workshop.CompleteClear);
        _model.WorkshopClearFailed += OnMainThread<string>(_workshop.FailClear);
    }

    private Action OnMainThread(Action action)
        => () => _runOnMainThread(action);

    private Action<T> OnMainThread<T>(Action<T> action)
        => value => _runOnMainThread(() => action(value));

    private void AppendCloudLog(string message)
    {
        if (message.StartsWith("[Cloud]"))
            _runOnMainThread(() => _view.AppendLog(message));
    }
}
