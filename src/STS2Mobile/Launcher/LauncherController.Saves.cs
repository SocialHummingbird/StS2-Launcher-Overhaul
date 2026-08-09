using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly record struct SaveSyncPresentation(
        string Headline,
        string LastSuccess,
        string LocalState,
        string SteamState
    );

    private int _saveSyncRunning;
    private int _saveSyncUiAvailable = 1;
    private int _launchAfterSaveSyncPending;
    private int _automaticSaveSyncStarted;
    private CancellationTokenSource _saveSyncCancellation;
    private Task _saveSyncTask = Task.CompletedTask;
    private SaveSyncPresentation _saveSyncPresentation;
    private SaveSyncPresentation _saveSyncPresentationBeforeOperation;
    private bool _saveSyncPresentationInitialized;

    private void LaunchPressed()
        => LaunchAfterSaveSync(_launch.LaunchPressed);

    private void SafeLaunchPressed()
        => LaunchAfterSaveSync(_launch.SafeLaunchPressed);

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
            PresentationFromStatus(SaveSyncService.GetStatusSnapshot())
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
                PresentationFromStatus(SaveSyncService.GetStatusSnapshot())
            );
            return;
        }

        if (!_saveSyncPresentationInitialized)
        {
            ApplySaveSyncPresentation(
                PresentationFromStatus(SaveSyncService.GetStatusSnapshot())
            );
        }
        _saveSyncPresentationBeforeOperation = _saveSyncPresentation;
        _view.SetSaveSyncControlsDisabled(true);
        ApplySaveSyncPresentation(
            SyncingPresentation(SaveSyncService.GetStatusSnapshot())
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
                ConflictPresentation(SaveSyncService.GetStatusSnapshot())
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
            var action = request == SaveSyncService.SyncRequest.Pull
                ? "Pull from Steam"
                : "Push to Steam";
            var confirmation = request == SaveSyncService.SyncRequest.Pull
                ? "Steam saves differ from this device. Pulling will replace or delete local save files. Continue?"
                : "This device differs from Steam. Pushing will replace or delete Steam save files. Continue?";
            RestoreSaveSyncPresentationBeforeOperation();
            _view.ShowConfirmation(
                confirmation,
                () => StartSaveSync(request, overwriteConfirmed: true),
                RestoreSaveSyncPresentationBeforeOperation,
                confirmText: action,
                cancelText: "Cancel"
            );
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
                FailedPresentation(SaveSyncService.GetStatusSnapshot())
            );
            return;
        }

        ApplySaveSyncPresentation(
            PresentationFromStatus(SaveSyncService.GetStatusSnapshot())
        );
    }

    private void FinishSaveSyncFailure()
    {
        _view.SetSaveSyncControlsDisabled(false);
        ApplySaveSyncPresentation(
            FailedPresentation(SaveSyncService.GetStatusSnapshot())
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

    private void ApplySaveSyncPresentation(SaveSyncPresentation presentation)
    {
        _saveSyncPresentation = presentation;
        _saveSyncPresentationInitialized = true;
        _view.SetSaveSyncPresentation(
            presentation.Headline,
            presentation.LastSuccess,
            presentation.LocalState,
            presentation.SteamState
        );
    }

    private void RestoreSaveSyncPresentationBeforeOperation()
        => ApplySaveSyncPresentation(_saveSyncPresentationBeforeOperation);

    private static SaveSyncPresentation PresentationFromStatus(
        SaveSyncService.StatusSnapshot status
    )
    {
        if (!status.HasCredentials)
            return SignInPresentation(status);

        if (status.ChangesQueued || status.RetryRequired)
            return OfflinePresentation(status);

        if (status.HasSuccessfulSync)
        {
            return new SaveSyncPresentation(
                "Synced",
                LastSuccess(status),
                "Up to date",
                "Up to date when last checked"
            );
        }

        // A signed-in first run immediately starts one reconciliation pass.
        // Until that pass completes, this is the only honest non-success state.
        return OfflinePresentation(status);
    }

    private static SaveSyncPresentation SyncingPresentation(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            "Syncing",
            LastSuccess(status),
            "Checking",
            "Checking"
        );

    private static SaveSyncPresentation ConflictPresentation(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            "Conflict — choose a copy",
            LastSuccess(status),
            "Different copy",
            "Different copy"
        );

    private static SaveSyncPresentation FailedPresentation(
        SaveSyncService.StatusSnapshot status
    )
        => status.HasCredentials
            ? OfflinePresentation(status)
            : SignInPresentation(status);

    private static SaveSyncPresentation OfflinePresentation(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            "Offline — changes queued",
            LastSuccess(status),
            status.ChangesQueued
                ? "Changes queued"
                : status.RetryRequired ? "Update queued" : "Saved locally",
            "Unavailable"
        );

    private static SaveSyncPresentation SignInPresentation(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            "Sign in required",
            LastSuccess(status),
            "Saved locally",
            "Sign in to check"
        );

    private static string LastSuccess(SaveSyncService.StatusSnapshot status)
    {
        if (status.LastSuccessfulSyncUtc is { } completedAt)
        {
            return completedAt.ToLocalTime().ToString(
                "g",
                CultureInfo.CurrentCulture
            );
        }

        return status.HasSuccessfulSync ? "Earlier" : "Never";
    }

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

        _ = CompleteLaunchAfterSaveSyncAsync(syncTask, launch);
    }

    private async Task CompleteLaunchAfterSaveSyncAsync(Task syncTask, Action launch)
    {
        try
        {
            await syncTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] Manual synchronization cleanup failed: {ex.GetType().Name}");
        }

        _runOnMainThread(() =>
        {
            Interlocked.Exchange(ref _launchAfterSaveSyncPending, 0);
            if (Volatile.Read(ref _saveSyncUiAvailable) != 0)
                launch();
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
