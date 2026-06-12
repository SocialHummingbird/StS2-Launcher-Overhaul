using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void WireModelEvents()
    {
        _model.SessionStateChanged += OnMainThread<LauncherModel.SessionState>(UpdateUI);
        _model.LogReceived += OnMainThread<string>(_view.AppendLog);
        PatchHelper.LogEmitted += AppendCloudLog;
        _model.CodeNeeded += OnMainThread<bool>(ShowCodePrompt);
        _model.DownloadProgressChanged += OnMainThread<DepotDownloader.DownloadProgress>(
            UpdateDownloadProgress
        );
        _model.DownloadLogReceived += OnMainThread<string>(_view.AppendLog);
        _model.DownloadCompleted += OnMainThread(CompleteDownload);
        _model.DownloadFailed += OnMainThread<string>(FailDownload);
        _model.DownloadCancelled += OnMainThread(CancelDownload);
        _model.UpdateCheckCompleted += OnMainThread<bool>(CompleteUpdateCheck);
        _model.UpdateCheckFailed += OnMainThread<string>(FailUpdateCheck);
        _model.BranchCatalogRefreshCompleted += OnMainThread(CompleteBranchCatalogRefresh);
        _model.BranchCatalogRefreshFailed += OnMainThread<string>(FailBranchCatalogRefresh);
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
