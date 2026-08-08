#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal async Task<LauncherAutomaticSyncPreparation> PrepareForLaunchAsync(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        bool beginGameSession
    )
    {
        var automaticSyncEnabled = LauncherCloudSaveState.CloudSyncEnabled;
        if (_operations.IsActive)
        {
            return LauncherAutomaticSyncPreparation.Blocked(
                "Launch blocked: a Steam Cloud operation is already running."
            );
        }

        if (CloudSyncCoordinator.HasSaveRecoverySyncHold())
        {
            return CloudSyncCoordinator.CanLaunchLocalRecoveryValidation(
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                out var recoveryMessage
            )
                ? LauncherAutomaticSyncPreparation.Ready(recoveryMessage)
                : LauncherAutomaticSyncPreparation.Blocked(recoveryMessage);
        }

        bool pending;
        try
        {
            pending = LauncherCloudSaveState.HasAutomaticSyncPending();
        }
        catch (Exception ex)
        {
            return LauncherAutomaticSyncPreparation.Blocked(
                $"Launch blocked: pending save-sync state could not be inspected ({CleanAutomaticSyncMessage(ex.Message)})."
            );
        }

        if (!automaticSyncEnabled && !pending)
        {
            return LauncherAutomaticSyncPreparation.Ready(
                "Automatic Steam save synchronization is disabled."
            );
        }

        _view.SetAutomaticSyncBlocked(true);
        _view.SetStatus("Reconciling Android and Steam saves before launch...");
        _view.AppendLog("Automatic save sync: checking Android and Steam before launch.");

        if (!_model.RefreshCloudSaveCredentials())
        {
            return FinishAutomaticLaunchPreparation(
                LauncherAutomaticSyncPreparation.Blocked(
                    "Launch blocked: no saved Steam login is available. Log in again before automatic save sync."
                ),
                keepBlocked: false
            );
        }

        LauncherAutomaticSyncPreparation preparation = default;
        if (!_operations.TryRun(
            async cancellationToken =>
            {
                preparation = await ExecuteAutomaticLaunchPreparationAsync(
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    beginGameSession,
                    automaticSyncEnabled,
                    cancellationToken
                ).ConfigureAwait(false);
            },
            out var execution
        ))
        {
            return FinishAutomaticLaunchPreparation(
                LauncherAutomaticSyncPreparation.Blocked(
                    "Launch blocked: another Steam Cloud operation started first."
                ),
                keepBlocked: false
            );
        }

        try
        {
            await execution.ConfigureAwait(false);
            return FinishAutomaticLaunchPreparation(
                preparation,
                keepBlocked: false
            );
        }
        catch (OperationCanceledException)
        {
            return FinishAutomaticLaunchPreparation(
                LauncherAutomaticSyncPreparation.Blocked(
                    "Launch blocked: automatic save synchronization was cancelled."
                ),
                keepBlocked: false
            );
        }
        catch (Exception ex)
        {
            return FinishAutomaticLaunchPreparation(
                LauncherAutomaticSyncPreparation.Blocked(
                    $"Launch blocked: automatic save synchronization failed ({CleanAutomaticSyncMessage(ex.Message)})."
                ),
                keepBlocked: false
            );
        }
    }

    internal void RecoverAutomaticSyncOnStartup()
    {
        if (_disposed || _model.InGameMode || _operations.IsActive)
            return;

        if (CloudSyncCoordinator.HasSaveRecoverySyncHold())
        {
            var recovery = CloudSyncCoordinator.InspectSaveRecoveryStatus();
            _view.SetAutomaticSyncBlocked(false);
            _view.SetStatus(recovery.Message);
            _view.AppendLog(
                "Automatic save sync remains off while recovered saves are validated locally."
            );
            return;
        }

        bool pending;
        try
        {
            pending = LauncherCloudSaveState.HasAutomaticSyncPending();
        }
        catch (Exception ex)
        {
            ShowAutomaticRecoveryBlocked(
                $"Could not inspect pending automatic save sync: {CleanAutomaticSyncMessage(ex.Message)}"
            );
            return;
        }

        if (!pending)
        {
            _view.SetAutomaticSyncBlocked(false);
            return;
        }

        _view.SetAutomaticSyncBlocked(true);
        _view.SetStatus("Recovering the pending Android and Steam save sync...");
        _view.AppendLog("Automatic save sync: resuming the pending post-game reconciliation.");
        if (!_model.RefreshCloudSaveCredentials())
        {
            ShowAutomaticRecoveryBlocked(
                "Pending save sync needs a Steam login. Log in again; recovery will retry automatically."
            );
            return;
        }

        _ = RecoverPendingAutomaticSyncAsync();
    }

    private async Task<LauncherAutomaticSyncPreparation>
        ExecuteAutomaticLaunchPreparationAsync(
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            bool beginGameSession,
            bool automaticSyncEnabled,
            CancellationToken cancellationToken
        )
    {
        var progress = NewAutomaticSyncProgress();
        if (LauncherCloudSaveState.HasAutomaticSyncPending())
        {
            var recovery = await LauncherCloudSaveState.RecoverAutomaticSyncAsync(
                progress,
                cancellationToken
            ).ConfigureAwait(false);
            if (recovery.Outcome != AutomaticSyncOutcome.Synchronized)
            {
                return LauncherAutomaticSyncPreparation.Blocked(
                    AutomaticLaunchBlockedMessage(recovery)
                );
            }
        }

        if (!automaticSyncEnabled)
        {
            return LauncherAutomaticSyncPreparation.Ready(
                "Pending save synchronization recovered; automatic Steam save synchronization is disabled."
            );
        }

        var result = await LauncherCloudSaveState.ReconcileAutomaticSyncAsync(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            sourceChoice: null,
            progress,
            cancellationToken
        ).ConfigureAwait(false);

        if (result.Outcome == AutomaticSyncOutcome.SourceChoiceRequired)
        {
            var sourceChoice = await RequestAutomaticSyncSourceChoiceAsync(
                result.Message,
                cancellationToken
            ).ConfigureAwait(false);
            result = await LauncherCloudSaveState.ReconcileAutomaticSyncAsync(
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                sourceChoice,
                progress,
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (result.Outcome != AutomaticSyncOutcome.Synchronized)
        {
            return LauncherAutomaticSyncPreparation.Blocked(
                AutomaticLaunchBlockedMessage(result)
            );
        }

        if (!beginGameSession)
            return LauncherAutomaticSyncPreparation.Ready(result.Message);

        result = await LauncherCloudSaveState.BeginAutomaticGameSessionAsync(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            progress,
            cancellationToken
        ).ConfigureAwait(false);
        return result.CanStartGame
            ? LauncherAutomaticSyncPreparation.Ready(result.Message)
            : LauncherAutomaticSyncPreparation.Blocked(
                AutomaticLaunchBlockedMessage(result)
            );
    }

    private async Task RecoverPendingAutomaticSyncAsync()
    {
        AutomaticSyncResult recovery = default;
        if (!_operations.TryRun(
            async cancellationToken =>
            {
                recovery = await LauncherCloudSaveState.RecoverAutomaticSyncAsync(
                    NewAutomaticSyncProgress(),
                    cancellationToken
                ).ConfigureAwait(false);
            },
            out var execution
        ))
        {
            return;
        }

        try
        {
            await execution.ConfigureAwait(false);
            if (recovery.Outcome == AutomaticSyncOutcome.Synchronized)
            {
                RunOnMainThread(() =>
                {
                    _view.SetAutomaticSyncBlocked(false);
                    _view.ClearCloudOperationState();
                    _view.SetStatus(recovery.Message);
                    _view.AppendLog($"Automatic save sync recovered: {recovery.Message}");
                });
                return;
            }

            ShowAutomaticRecoveryBlocked(recovery.Message);
        }
        catch (OperationCanceledException)
        {
            ShowAutomaticRecoveryBlocked(
                "Pending automatic save sync remains on disk because recovery was cancelled."
            );
        }
        catch (Exception ex)
        {
            ShowAutomaticRecoveryBlocked(
                "Pending automatic save sync remains on disk: "
                    + CleanAutomaticSyncMessage(ex.Message)
            );
        }
    }

    private CloudOperationProgressTracker NewAutomaticSyncProgress()
        => new(CloudOperationKind.Push, ReportCloudOperationState);

    private async Task<AutomaticSyncSourceChoice>
        RequestAutomaticSyncSourceChoiceAsync(
            string reason,
            CancellationToken cancellationToken
        )
    {
        var selected = new TaskCompletionSource<AutomaticSyncSourceChoice>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var prompt = CleanAutomaticSyncMessage(reason)
            + "\n\nNo trusted baseline exists for this exact account, game version, and mod set. "
            + "Use Android uploads the Android copy. Use Steam backs up Android first, then downloads the Steam copy.";
        RunOnMainThread(() =>
            _view.ShowAutomaticSyncSourceChoice(
                prompt,
                choice => selected.TrySetResult(choice)
            )
        );
        return await selected.Task.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private LauncherAutomaticSyncPreparation FinishAutomaticLaunchPreparation(
        LauncherAutomaticSyncPreparation preparation,
        bool keepBlocked
    )
    {
        RunOnMainThread(() =>
        {
            _view.SetAutomaticSyncBlocked(keepBlocked);
            _view.ClearCloudOperationState();
            if (!string.IsNullOrWhiteSpace(preparation.Message))
                _view.AppendLog($"Automatic save sync: {preparation.Message}");
        });
        return preparation;
    }

    private void ShowAutomaticRecoveryBlocked(string message)
    {
        var clean = CleanAutomaticSyncMessage(message);
        RunOnMainThread(() =>
        {
            // The pending record still gates every Play attempt. Re-enable the
            // button so Play itself is also a visible, safe recovery retry.
            _view.SetAutomaticSyncBlocked(false);
            _view.ClearCloudOperationState();
            _view.SetStatus(clean);
            _view.AppendLog($"Automatic save sync blocked: {clean}");
        });
    }

    private static string AutomaticLaunchBlockedMessage(
        AutomaticSyncResult result
    )
    {
        var reason = CleanAutomaticSyncMessage(result.Message);
        return result.Outcome switch
        {
            AutomaticSyncOutcome.Conflict
                => $"Launch blocked by an Android/Steam save conflict: {reason}",
            AutomaticSyncOutcome.PendingRecoveryRequired
                => $"Launch blocked until the pending save sync is recovered: {reason}",
            AutomaticSyncOutcome.SourceChoiceRequired
                => $"Launch blocked until a save source is chosen: {reason}",
            _ => $"Launch blocked because automatic save sync did not complete: {reason}",
        };
    }

    private static string CleanAutomaticSyncMessage(string? message)
        => string.IsNullOrWhiteSpace(message)
            ? "No additional detail was provided."
            : message.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
