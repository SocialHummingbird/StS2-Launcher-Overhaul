using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private int _saveSyncRunning;
    private int _saveSyncUiAvailable = 1;
    private int _launchAfterSaveSyncPending;
    private int _automaticSaveSyncStarted;
    private CancellationTokenSource _saveSyncCancellation;
    private Task _saveSyncTask = Task.CompletedTask;
    private LauncherSaveSyncPresentation _saveSyncPresentation;
    private LauncherSaveSyncPresentation _saveSyncPresentationBeforeOperation;
    private bool _saveSyncPresentationInitialized;

    private void LaunchPressed()
        => LaunchAfterSaveSync(() => LaunchOrRepair(_launch.LaunchPressed));

    private void SafeLaunchPressed()
        => LaunchAfterSaveSync(() => LaunchOrRepair(_launch.SafeLaunchPressed));

    private void LaunchOrRepair(Action launch)
    {
        if (_downloads.TryStartAutomaticRepair(launch)
            || _downloads.TryShowRedownloadRequired())
        {
            return;
        }

        launch();
    }

    private void SessionRetryPressed()
    {
        var fastPathReady = _session.RetryPressed();
        RefreshSaveSyncPresentation();
        if (fastPathReady)
            StartAutomaticSaveSync();
    }

    internal void CancelSaveSyncForLauncherExit()
    {
        Volatile.Write(ref _saveSyncUiAvailable, 0);
        CancelActiveSaveSync();
    }

    private void SaveSyncNowPressed()
        => StartSaveSync(SaveSyncService.SyncRequest.Reconcile, overwriteConfirmed: false);

    private void SavePullPressed()
        => StartSaveSync(SaveSyncService.SyncRequest.Pull, overwriteConfirmed: false);

    private void SavePushPressed()
        => StartSaveSync(SaveSyncService.SyncRequest.Push, overwriteConfirmed: false);

    private void StartAutomaticSaveSync()
    {
        if (Interlocked.CompareExchange(ref _automaticSaveSyncStarted, 1, 0) != 0)
            return;

        StartSaveSync(
            SaveSyncService.SyncRequest.Reconcile,
            overwriteConfirmed: false
        );
    }

    private void RefreshSaveSyncPresentation()
    {
        if (Volatile.Read(ref _saveSyncRunning) != 0)
            return;

        ApplySaveSyncPresentation(
            LauncherSaveSyncPresentation.FromStatus(
                SaveSyncService.GetStatusSnapshot()
            )
        );
    }

    private void StartSaveSync(
        SaveSyncService.SyncRequest request,
        bool overwriteConfirmed
    )
    {
        if (Interlocked.CompareExchange(ref _saveSyncRunning, 1, 0) != 0)
            return;

        if (!SaveSyncService.TryGetActive(out var service))
        {
            Interlocked.Exchange(ref _saveSyncRunning, 0);
            ApplySaveSyncPresentation(
                LauncherSaveSyncPresentation.FromStatus(
                    SaveSyncService.GetStatusSnapshot()
                )
            );
            return;
        }

        if (!_saveSyncPresentationInitialized)
        {
            ApplySaveSyncPresentation(
                LauncherSaveSyncPresentation.FromStatus(
                    SaveSyncService.GetStatusSnapshot()
                )
            );
        }
        _saveSyncPresentationBeforeOperation = _saveSyncPresentation;
        _view.SetSaveSyncControlsDisabled(true);
        ApplySaveSyncPresentation(
            LauncherSaveSyncPresentation.Syncing()
        );
        var cancellation = new CancellationTokenSource();
        _saveSyncCancellation = cancellation;
        _saveSyncTask = RunSaveSyncAsync(
            service,
            request,
            overwriteConfirmed,
            cancellation.Token
        );
    }

    private async Task RunSaveSyncAsync(
        SaveSyncService service,
        SaveSyncService.SyncRequest request,
        bool overwriteConfirmed,
        CancellationToken cancellationToken
    )
    {
        SaveSyncService.SyncResult result;
        try
        {
            result = await Task.Run(() => service.SyncAsync(
                request,
                overwriteConfirmed,
                localWritesAreStopped: true,
                cancellationToken
            )).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Manual synchronization task failed: {ex.GetType().Name}"
            );
            ReleaseSaveSyncOperation();
            QueueSaveSyncUiCompletion(FinishSaveSyncFailure);
            return;
        }

        ReleaseSaveSyncOperation();
        QueueSaveSyncUiCompletion(() => FinishSaveSync(request, result));
    }

    private void FinishSaveSync(
        SaveSyncService.SyncRequest request,
        SaveSyncService.SyncResult result
    )
    {
        _view.SetSaveSyncControlsDisabled(false);

        if (result.Prompt == SaveSyncService.SyncPrompt.ChooseSource)
        {
            ApplySaveSyncPresentation(
                LauncherSaveSyncPresentation.Conflict(
                    SaveSyncService.GetStatusSnapshot()
                )
            );
            _view.ShowSaveSyncConflict(
                () => StartSaveSync(
                    SaveSyncService.SyncRequest.Pull,
                    overwriteConfirmed: true
                ),
                () => StartSaveSync(
                    SaveSyncService.SyncRequest.Push,
                    overwriteConfirmed: true
                )
            );
            return;
        }

        if (result.Prompt == SaveSyncService.SyncPrompt.ConfirmOverwrite)
        {
            RestoreSaveSyncPresentationBeforeOperation();
            if (request == SaveSyncService.SyncRequest.Pull)
            {
                _view.ShowGetSavesOverwriteConfirmation(
                    () => StartSaveSync(request, overwriteConfirmed: true),
                    RestoreSaveSyncPresentationBeforeOperation
                );
            }
            else
            {
                _view.ShowSendSavesOverwriteConfirmation(
                    () => StartSaveSync(request, overwriteConfirmed: true),
                    RestoreSaveSyncPresentationBeforeOperation
                );
            }
            return;
        }

        if (result.Canceled)
        {
            RestoreSaveSyncPresentationBeforeOperation();
            return;
        }

        if (!result.Success)
        {
            ApplySaveSyncPresentation(
                result.FailureKind == SaveSyncService.SyncFailureKind.Offline
                    ? LauncherSaveSyncPresentation.Offline(
                        SaveSyncService.GetStatusSnapshot()
                    )
                    : result.FailureKind
                        == SaveSyncService.SyncFailureKind.Authentication
                        ? LauncherSaveSyncPresentation.SignInRequired()
                        : LauncherSaveSyncPresentation.SyncFailed(
                        SaveSyncService.GetStatusSnapshot()
                    )
            );
            return;
        }

        ApplySaveSyncPresentation(
            LauncherSaveSyncPresentation.FromStatus(
                SaveSyncService.GetStatusSnapshot()
            )
        );
    }

    private void FinishSaveSyncFailure()
    {
        SaveSyncService.ReportFailure(SaveSyncService.SyncFailureKind.Other);
        _view.SetSaveSyncControlsDisabled(false);
        ApplySaveSyncPresentation(
            LauncherSaveSyncPresentation.SyncFailed(
                SaveSyncService.GetStatusSnapshot()
            )
        );
    }

    private void QueueSaveSyncUiCompletion(Action completion)
    {
        if (Volatile.Read(ref _saveSyncUiAvailable) == 0)
            return;

        _runOnMainThread(() =>
        {
            if (Volatile.Read(ref _saveSyncUiAvailable) != 0)
                completion();
        });
    }

    private void ApplySaveSyncPresentation(LauncherSaveSyncPresentation presentation)
    {
        _saveSyncPresentation = presentation;
        _saveSyncPresentationInitialized = true;
        _view.SetSaveSyncPresentation(presentation);
    }

    private void RestoreSaveSyncPresentationBeforeOperation()
        => ApplySaveSyncPresentation(_saveSyncPresentationBeforeOperation);

    private void LaunchAfterSaveSync(Action launch)
    {
        if (Interlocked.CompareExchange(ref _launchAfterSaveSyncPending, 1, 0) != 0)
            return;

        var syncTask = CancelActiveSaveSync();
        if (syncTask == null || syncTask.IsCompleted)
        {
            Interlocked.Exchange(ref _launchAfterSaveSyncPending, 0);
            launch();
            return;
        }

        _view.SetStatus("Finishing save synchronization before launch...", LauncherStatusSeverity.Working);
        _ = CompleteLaunchAfterSaveSyncAsync(syncTask, launch);
    }

    private async Task CompleteLaunchAfterSaveSyncAsync(Task syncTask, Action launch)
    {
        var cleanupFinished = true;
        try
        {
            cleanupFinished = await LauncherSaveCleanup.WaitAsync(syncTask, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] Manual synchronization cleanup failed: {ex.GetType().Name}");
        }

        _runOnMainThread(() =>
        {
            Interlocked.Exchange(ref _launchAfterSaveSyncPending, 0);
            if (Volatile.Read(ref _saveSyncUiAvailable) != 0)
            {
                if (cleanupFinished) launch();
                else _view.SetStatus("Save synchronization is still finishing. Launch was stopped to protect your saves. Wait, then press Play again.", LauncherStatusSeverity.Warning);
            }
        });
    }

    private Task CancelActiveSaveSync()
    {
        try
        {
            Volatile.Read(ref _saveSyncCancellation)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        return Volatile.Read(ref _saveSyncTask);
    }

    private void ReleaseSaveSyncOperation()
    {
        Interlocked.Exchange(ref _saveSyncRunning, 0);
        Interlocked.Exchange(ref _saveSyncCancellation, null)?.Dispose();
    }
}
