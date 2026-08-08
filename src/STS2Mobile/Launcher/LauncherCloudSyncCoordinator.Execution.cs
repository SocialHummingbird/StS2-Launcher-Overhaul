#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal void CloudPullPressed()
    {
        if (RejectWhenOperationActive())
            return;

        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull,
            ReportCloudOperationState
        );
        progress.Preparing("Checking saved Steam login");
        RequestCloudSync(ManualCloudSyncRequest.Pull(
            _model.DataDir,
            LauncherPreferences.ReadGameBranch(),
            progress
        ));
    }

    internal void CloudOperationCancelPressed()
    {
        if (!_operations.IsActive)
            return;

        const string message =
            "Cancelling Steam Cloud operation and waiting for active file work to stop...";
        RunOnMainThread(() =>
        {
            _view.SetStatus(message);
            _view.AppendLog(message);
        });
        _ = _operations.CancelAndDrainAsync();
    }

    private void RequestCloudSync(ManualCloudSyncRequest request)
    {
        if (request.BypassConfirmation)
        {
            _ = StartCloudSync(request);
            return;
        }

        _view.ShowConfirmation(
            request.ConfirmationMessage,
            () => _ = StartCloudSync(request),
            request.ConfirmText,
            request.CancelText
        );
    }

    private Task StartCloudSync(ManualCloudSyncRequest request)
    {
        if (!_operations.TryRun(
            cancellationToken => ExecuteCloudSyncAsync(
                request,
                cancellationToken
            ),
            out var execution
        ))
        {
            ShowOperationAlreadyRunning();
            return Task.CompletedTask;
        }

        return execution;
    }

    private async Task ExecuteCloudSyncAsync(
        ManualCloudSyncRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            request.PrepareOrThrow();
            _model.RefreshCloudSaveCredentials();
            RunOnMainThread(() => request.ShowStarted(_view));

            var result = await request.RunWithTimeoutAsync(
                cancellationToken
            ).ConfigureAwait(false);
            RunOnMainThread(
                () => CompleteCloudSync(request, result)
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            if (!_disposed)
            {
                RunOnMainThread(
                    () => FinishCloudSync(
                        request,
                        CloudOperationTerminalOutcome.Cancelled,
                        "Operation cancelled"
                    )
                );
            }
        }
        catch (TimeoutException ex)
        {
            if (!_disposed)
            {
                RunOnMainThread(
                    () => FinishCloudSync(
                        request,
                        CloudOperationTerminalOutcome.Timeout,
                        ex.Message,
                        ex
                    )
                );
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                RunOnMainThread(
                    () => FinishCloudSync(
                        request,
                        CloudOperationTerminalOutcome.Failure,
                        ex.Message,
                        ex
                    )
                );
            }
        }
    }

    private void CompleteCloudSync(
        ManualCloudSyncRequest request,
        ManualCloudSyncResult result
    )
    {
        try
        {
            var resolution = request.ResolveCompletion(
                result,
                CaptureCurrentState
            );
            ApplyPostOperationSnapshot(resolution.Snapshot);
            request.ShowTerminal(
                _view,
                CloudOperationTerminalPresentation.CreateCompletion(
                    resolution
                )
            );
        }
        catch (Exception ex)
        {
            FinishCloudSync(
                request,
                CloudOperationTerminalOutcome.Failure,
                $"Post-operation refresh failed: {ex.Message}",
                ex,
                showFinished: false
            );
        }
        finally
        {
            request.ShowFinished(_view);
        }
    }

    private void FinishCloudSync(
        ManualCloudSyncRequest request,
        CloudOperationTerminalOutcome outcome,
        string reason,
        Exception? exception = null,
        bool showFinished = true
    )
    {
        try
        {
            request.RecordTerminalFailure(
                OutcomeMarker(outcome),
                reason,
                exception
            );
            var snapshot = CaptureCurrentState();
            ApplyPostOperationSnapshot(snapshot);

            var presentation = outcome switch
            {
                CloudOperationTerminalOutcome.Timeout
                    => CloudOperationTerminalPresentation.CreateTimeout(
                        request.OperationName,
                        request.ProgressState,
                        reason
                    ),
                CloudOperationTerminalOutcome.Cancelled
                    => CloudOperationTerminalPresentation.CreateCancelled(
                        request.OperationName,
                        request.ProgressState
                    ),
                _ => CloudOperationTerminalPresentation.CreateFailure(
                    request.OperationName,
                    request.ProgressState,
                    reason
                ),
            };
            request.ShowTerminal(_view, presentation);
        }
        finally
        {
            if (showFinished)
                request.ShowFinished(_view);
        }
    }

    private static string OutcomeMarker(
        CloudOperationTerminalOutcome outcome
    )
        => outcome switch
        {
            CloudOperationTerminalOutcome.Timeout => "timeout",
            CloudOperationTerminalOutcome.Cancelled => "cancelled",
            _ => "failure",
        };

    private void RunOnMainThread(Action action)
        => _runOnMainThread(() =>
        {
            if (!_disposed)
                action();
        });

    private void ReportCloudOperationState(CloudOperationState state)
        => RunOnMainThread(() => _view.SetCloudOperationState(state));

    private bool RejectWhenOperationActive()
    {
        if (!_operations.IsActive)
            return false;

        ShowOperationAlreadyRunning();
        return true;
    }

    private void ShowOperationAlreadyRunning()
    {
        const string message =
            "A Steam Cloud operation is already running. Wait for it to finish or cancel it before starting another.";
        RunOnMainThread(() =>
        {
            _view.SetStatus(message);
            _view.AppendLog(message);
        });
    }
}
