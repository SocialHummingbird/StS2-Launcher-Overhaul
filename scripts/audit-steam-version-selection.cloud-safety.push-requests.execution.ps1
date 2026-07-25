function Add-SteamVersionSelectionCloudSafetyPushExecutionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Lifecycle.cs" `
        "keeps manual cloud sync lifecycle UI updates and timeout execution isolated" `
        @(
            "ShowStarted",
            "ResolveCompletion",
            "RecordTerminalFailure",
            "ShowTerminal",
            "ShowFinished",
            "RunWithTimeoutAsync",
            "SetPushPullDisabled\(true\)",
            "SetPushPullDisabled\(false\)",
            "CloudPostOperationRefresh\.ResolveCompletion",
            "PatchHelper\.Log",
            "LauncherTimeout\.RunOrThrowAsync",
            "Task<ManualCloudSyncResult>",
            "TimeoutMs"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Execution.cs" `
        "executes Pull/Push cloud sync requests without bypassing Push confirmation" `
        @(
            "ManualCloudSyncRequest\.Pull\(",
            "CloudOperationProgressTracker",
            "CloudOperationKind\.Pull",
            "progress\.Preparing",
            "ReportCloudOperationState",
            "_view\.SetCloudOperationState",
            "RequestCloudSync",
            "ExecuteCloudSyncAsync",
            "request\.BypassConfirmation",
            "ShowConfirmation",
            "RunOnMainThread",
            "_operations\.TryRun",
            "CancellationToken cancellationToken",
            "_operations\.CancelAndDrainAsync",
            "A Steam Cloud operation is already running",
            "if \(!_disposed\)",
            "request\.PrepareOrThrow\(\)",
            "CompleteCloudSync",
            "FinishCloudSync",
            "ApplyPostOperationSnapshot",
            "request\.ShowFinished\(_view\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudOperationState.cs" `
        "models every observable Pull phase and cumulative operation count" `
        @(
            "CloudOperationKind",
            "CloudOperationPhase",
            "Preparing",
            "Enumerating",
            "BackingUp",
            "PreparingProfiles",
            "Transferring",
            "SeedingProfiles",
            "Finalizing",
            "Completed",
            "PartiallyCompleted",
            "Failed",
            "EnumeratedPathCount",
            "BackupProcessedCount",
            "BackupCreatedCount",
            "TransferProcessedCount",
            "TransferCompletedCount",
            "TransferSkippedCount",
            "TransferFailedCount",
            "TransferTimedOutCount",
            "ProfileSeedProcessedCount",
            "ProfileSeededCount",
            "IsActive",
            "IsTerminal"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudOperationProgressTracker.cs" `
        "publishes immutable snapshots for every Pull phase and count update" `
        @(
            "CloudOperationProgressTracker",
            "Action<CloudOperationState>",
            "EnumerationStarted",
            "EnumerationCompleted",
            "BackupStarted",
            "BackupPathStarted",
            "BackupProcessed",
            "ProfilePreparationStarted",
            "ProfilePreparationPathStarted",
            "ProfilePreparationProcessed",
            "TransferStarted",
            "TransferPathStarted",
            "TransferProcessed",
            "ProfileSeedingStarted",
            "ProfileSeedPathStarted",
            "ProfileSeedProcessed",
            "Finalizing",
            "Completed",
            "PartiallyCompleted",
            "Failed",
            "_publish\?\.Invoke\(_state\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs" `
        "reports preparation, enumeration, backup, completion, and failure from the production sync plan" `
        @(
            "progress\.Preparing",
            "ReportEnumerationStarted",
            "ReportEnumerationCompleted",
            "ReportBackupStarted",
            "progress\.Completed",
            "progress\.Failed"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Pull.cs" `
        "reports each production Pull transfer and finalization step" `
        @(
            "ReportTransferStarted\(paths\.Count\)",
            "ReportTransferPathStarted\(path\)",
            "ReportTransferProcessed",
            "result\.Outcome",
            "ManualSyncTransferSummary",
            "BuildResult",
            "ReportFinalizing"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.ModdedSaveSeed.cs" `
        "reports private target preparation and every modded profile seed decision" `
        @(
            "ReportProfilePreparationStarted",
            "ReportProfilePreparationPathStarted",
            "ReportProfilePreparationProcessed",
            "backupCreated: false",
            "backupCreated: true",
            "ReportProfileSeedingStarted",
            "ReportProfileSeedPathStarted",
            "ReportProfileSeedProcessed",
            "seeded: false",
            "seeded: true"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.ManualRunner.cs" `
        "publishes the current backup path before each potentially slow backup read or write" `
        @(
            "Action<string>\? reportStarted",
            "Action<string, bool>\? reportProcessed",
            "reportStarted\?\.Invoke\(path\)",
            "await plan\.TryBackupPathAsync\(",
            "cancellationToken",
            "reportProcessed\?\.Invoke\(path, created\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Manual.cs" `
        "connects production backup passes to current-path and count reporting" `
        @(
            "sync\.ReportBackupPathStarted",
            "sync\.ReportBackupProcessed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudOperationPresentation.cs" `
        "turns every Pull state into visible current-phase, count, item, and completion text" `
        @(
            "CloudOperationPresentation",
            "Finding Steam Cloud saves",
            "Backing up Android saves",
            "Protecting modded profiles",
            "Downloading Steam Cloud saves",
            "Preparing modded profiles",
            'Finishing \{operation\}',
            '\{operation\} complete',
            "PartiallyCompleted",
            '\{operation\} stopped',
            "TransferProcessedCount",
            "TransferCompletedCount",
            "TransferTimedOutCount",
            "ProfileSeededCount",
            "Current:",
            "ProgressFor",
            "TransferTotalCount"
        )

    Add-Check `
        "scripts\test-launcher-cloud-pull-progress.ps1" `
        "runs focused observable Pull state and presentation tests" `
        @(
            "CloudOperationState\.cs",
            "CloudOperationProgressTracker\.cs",
            "CloudOperationPresentation\.cs",
            "LauncherCloudPullProgressTest\.cs",
            "TreatWarningsAsErrors>true",
            "dotnet\.Source run"
        )

    Add-Check `
        "scripts\tests\LauncherCloudCurrentBehaviorRegressionTest.cs" `
        "replaces the original silent-Pull reproduction with a coordinator-level progress contract" `
        @(
            "VerifyObservablePullProgressAsync",
            "ExecuteSyntheticPullRequestAsync",
            "CloudOperationPhase\.Transferring",
            "pendingState\.IsActive",
            "pendingState\.TransferTotalCount",
            "view\.CloudOperationStates\.Count >= 6",
            "CloudOperationPhase\.Completed",
            "\[FIXED\] Pull exposes live cloud-operation phases and counts"
        )

    Add-Check `
        "tools\LauncherUiPreview\PreviewRoot.cs" `
        "provides realistic active and completed Pull fixtures for rendered UI validation" `
        @(
            "pull-transfer",
            "pull-complete",
            "ActivePullState",
            "CompletedPullState",
            "SetCloudOperationState",
            "TransferPathStarted",
            "ProfileSeedProcessed",
            "tracker\.Completed"
        )

    Add-Check `
        "tools\LauncherUiPreview\LauncherCloudProgressPreviewValidator.cs" `
        "checks rendered Pull phase, counts, current item, failures, seeds, and progress" `
        @(
            "Downloading Steam Cloud saves",
            "11/24 checked",
            "8 downloaded",
            "Current: profile2/saves/progress\.save",
            "Pull complete",
            "24/24 checked",
            "1 failed",
            "4 modded file\(s\) seeded",
            "progress\.Value",
            "Cancel Cloud Operation",
            "expectedVisible: true",
            "expectedVisible: false",
            "button\.Disabled"
        )

    Add-Check `
        "scripts\test-launcher-ui-preview.ps1" `
        "renders active and completed Pull progress across desktop, landscape, and portrait layouts" `
        @(
            'Fixture = "pull-transfer"; Destination = "saves"; Width = 1280; Height = 800',
            'Fixture = "pull-complete"; Destination = "saves"; Width = 1280; Height = 800',
            'Fixture = "pull-transfer"; Destination = "saves"; Width = 2400; Height = 1080',
            'Fixture = "pull-transfer"; Destination = "saves"; Width = 1080; Height = 2400',
            'Fixture = "pull-complete"; Destination = "saves"; Width = 1080; Height = 2400'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudOperationSessionManager.cs" `
        "owns exactly one cancellable cloud operation and drains it before release or disposal" `
        @(
            "CloudOperationSessionManager : IDisposable",
            "if \(_disposed \|\| _active != null\)",
            "_active = operation",
            "await run\(operation\.CancellationToken\)",
            "operation\?\.Cancel\(\)",
            "await operation\.Drained",
            "operation\?\.Drained\.GetAwaiter\(\)\.GetResult\(\)",
            "_active = null",
            "_cancellation\.Dispose\(\)",
            "_drained\.TrySetResult\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherTimeout.cs" `
        "cancels token-aware cloud work on timeout and awaits worker cleanup" `
        @(
            "Func<CancellationToken, Task<T>> operation",
            "CreateLinkedTokenSource",
            "timeoutCancellation\.CancelAfter\(timeoutMs\)",
            "await operation\(timeoutCancellation\.Token\)",
            "!cancellationToken\.IsCancellationRequested",
            "throw new TimeoutException\(timeoutMessage, ex\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Lifecycle.cs" `
        "passes the owned cancellation token through timeout execution and exposes cancellation completion" `
        @(
            "RecordTerminalFailure",
            "CloudOperationState ProgressState",
            "Task<ManualCloudSyncResult>",
            "RunWithTimeoutAsync\(",
            "CancellationToken cancellationToken",
            "token => Task\.Run",
            "\(\) => run\(token\)",
            "LauncherTimeout\.RunOrThrowAsync",
            "cancellationToken"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.cs" `
        "disposes the active cloud-operation owner synchronously" `
        @(
            "LauncherCloudSyncCoordinator : IDisposable",
            "CloudOperationSessionManager _operations",
            "volatile bool _disposed",
            "CancelAndDrainAsync",
            "_disposed = true",
            "_operations\.Dispose\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.Lifecycle.cs" `
        "cancels and drains cloud work before model destruction" `
        @(
            "OnExitTree",
            "_controller\?\.Dispose\(\)",
            "_model\?\.Dispose\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
        "provides a neutral cloud-operation cancellation action only while work is active" `
        @(
            "Cancel Cloud Operation",
            "RequestCloudOperationCancellation",
            "cancelOperationButton\.Visible = false",
            "ApplySupportAction",
            "CancelOperationButton"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOperationProgress.cs" `
        "disables repeated cancellation taps and raises the cancellation event once" `
        @(
            "RequestCloudOperationCancellation",
            "_cancelCloudOperationButton\.Disabled = true",
            "Cancelling\.\.\.",
            "CloudOperationCancelPressed\?\.Invoke\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CancellableSaveStore.cs" `
        "uses cancellation-capable stores and drains legacy operations before observing cancellation" `
        @(
            "ICancellableSaveStore",
            "CancellationToken cancellationToken",
            "store is ICancellableSaveStore cancellable",
            "await cancellable\.ReadFileAsync",
            "await cancellable\.WriteFileAsync",
            "await store\.ReadFileAsync\(path\)",
            "await store\.WriteFileAsync\(path, content\)",
            "cancellationToken\.ThrowIfCancellationRequested\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CancellableAtomicFile.cs" `
        "stages cancellable writes and replaces destinations only after successful completion" `
        @(
            "CancellableAtomicFile",
            "WriteAllBytesAsync",
            "WriteAllTextAsync",
            "writeStaging\(stagingPath, cancellationToken\)",
            "cancellationToken\.ThrowIfCancellationRequested\(\)",
            "File\.Move\(stagingPath, destinationPath, overwrite\)",
            "finally",
            "File\.Delete\(stagingPath\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.OperationTimeout.cs" `
        "uses linked cancellation for every manual per-file cloud deadline" `
        @(
            "Func<CancellationToken, Task> operation",
            "Func<CancellationToken, Task<T>> operation",
            "CreateLinkedTokenSource",
            "CancelAfter\(TimeoutMs\)",
            "await operation\(timeoutCancellation\.Token\)",
            "throw new TimeoutException"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.OperationTimeout.cs" `
        "cannot return from a deadline while an uncancellable task continues" `
        @(
            "WaitForCompletionAsync",
            "LateCompletion",
            "ContinueWith",
            "Task\.WhenAny",
            "Task task"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Context.cs" `
        "threads the owned token through discovery, metadata, reads, writes, and mirror finalization" `
        @(
            "CancellationToken _cancellationToken",
            "SavePathDiscovery\.Get\(_local, _cancellationToken\)",
            "metadata\.PrepareFileMetadata\(_cancellationToken\)",
            "SavePathDiscovery\.Get\(_cloud, _cancellationToken\)",
            "CancellableSaveStore\.ReadFileAsync",
            "CancellableSaveStore\.WriteFileAsync",
            "WaitForCloudOperationAsync",
            "RefreshLocalMirror\(",
            "_cancellationToken"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Context.cs" `
        "does not expose synchronous manual reads or queued immediate-upload batches" `
        @(
            "internal string\? ReadLocalFile",
            "RunCloudBatchImmediate",
            "EndSaveBatchAndUploadNow",
            "FlushCloudWrites"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Push.cs" `
        "awaits each manual upload directly under the operation token" `
        @(
            "sync\.CancellationToken\.ThrowIfCancellationRequested\(\)",
            "await sync\.ReadLocalFileAsync\(path\)",
            "await sync\.WriteCloudFileAsync\(path, local\)",
            "catch \(OperationCanceledException\)",
            "when \(sync\.CancellationToken\.IsCancellationRequested\)"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Push.cs" `
        "cannot enqueue hidden manual uploads after Push returns" `
        @(
            "RunCloudBatchImmediate",
            "EndSaveBatchAndUploadNow",
            "EnqueueUpload",
            "CancellationToken\.None"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamConnection.Lifecycle.cs" `
        "propagates cancellation through Steam connection, reconnect backoff, and login waits" `
        @(
            "EnsureConnected\(",
            "CancellationToken cancellationToken = default",
            "WaitForBackoffIfNeeded\(cancellationToken\)",
            "cancellationToken\.WaitHandle\.WaitOne\(_backoffMs\)",
            "WaitForConnectionAttempt\(cancellationToken\)",
            "_connectedGate\.Wait\(",
            "cancellationToken",
            "Connect cancelled; resetting the pending attempt",
            "ResetAfterFailedConnect\(\)",
            "ResetConnectionTransport\(\)"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\SteamConnection.Lifecycle.cs" `
        "does not permanently consume final teardown while resetting a retryable connection attempt" `
        @(
            "private void ResetAfterFailedConnect\(\)[\s\S]{0,120}Teardown\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamConnection.Controls.cs" `
        "uses reusable transport reset for idle disconnects and final teardown only during disposal" `
        @(
            "DisconnectToIdle",
            "ResetConnectionTransport\(\)",
            "internal void Dispose\(\)",
            "var callbackPumpStopped = Teardown\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamConnection.Teardown.cs" `
        "keeps reusable Steam transport reset separate from one-shot final teardown" `
        @(
            "private bool Teardown\(\)",
            "_teardownStarted",
            "ResetConnectionTransport\(\)",
            "_teardownComplete\.Set\(\)",
            "private bool ResetConnectionTransport\(\)",
            "_client\?\.Disconnect\(\)",
            "_callbackPump\.Stop\(2000, clearThread: true\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamConnection.CloudMessages.cs" `
        "cancels Steam RPC waits by disconnecting and draining the pending job" `
        @(
            "EnsureConnected\(cancellationToken\)",
            "_sendLock\.WaitAsync\(cancellationToken\)",
            "WaitForCloudJobAsync",
            "cancellationToken\.ThrowIfCancellationRequested\(\)",
            "Task\.Delay\(",
            "cancellationToken",
            "AbortAndDrainCloudJobAsync",
            "_client\.Disconnect\(\)",
            "while \(!task\.IsCompleted\)",
            "await task\.ConfigureAwait\(false\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamKit2CloudSaveStore.Http.cs" `
        "propagates cancellation through cloud HTTP requests and response bodies" `
        @(
            "CancellationToken cancellationToken",
            "CreateLinkedTokenSource",
            "timeoutCancellation\.CancelAfter",
            "_http\.SendAsync\(",
            "timeoutCancellation\.Token",
            "ReadAsByteArrayAsync\(",
            "cancellationToken",
            "!cancellationToken\.IsCancellationRequested"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudFileCache.Loading.cs" `
        "does not convert cancelled cloud enumeration into retry failure" `
        @(
            "EnsureLoaded\(",
            "CancellationToken cancellationToken",
            "LoadFileList\(cancellationToken\)",
            "catch \(OperationCanceledException\)",
            "when \(cancellationToken\.IsCancellationRequested\)",
            "throw;"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamKit2CloudSaveStore.Files.cs" `
        "uses direct cancellable upload and updates metadata only after success" `
        @(
            "ICancellableSaveStore\.WriteFileAsync",
            "WriteFileCancellableAsync",
            "cancellationToken\.ThrowIfCancellationRequested\(\)",
            "await UploadWithRetryAsync",
            "_cache\.Set"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamKit2CloudSaveStore.Retry.cs" `
        "cancels upload retries and throttle delays" `
        @(
            "UploadWithRetryAsync",
            "CancellationToken cancellationToken",
            "cancellationToken\.ThrowIfCancellationRequested\(\)",
            "catch \(OperationCanceledException\)",
            "Task\.Delay\(",
            "cancellationToken"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Storage.cs" `
        "uses cancellable atomic storage for manual save backups" `
        @(
            "TrySaveAsync",
            "CancellationToken cancellationToken",
            "CancellableAtomicFile\.WriteAllTextAsync",
            "overwrite: false",
            "catch \(OperationCanceledException\)",
            "throw;"
        )

    Add-Check `
        "scripts\test-launcher-cloud-cancellation.ps1" `
        "runs cancellation, ownership, compatibility-drain, and atomic-write regressions as warnings-as-errors" `
        @(
            "LauncherTimeout\.cs",
            "CloudOperationSessionManager\.cs",
            "CancellableSaveStore\.cs",
            "CancellableAtomicFile\.cs",
            "LauncherCloudCancellationTest\.cs",
            "TreatWarningsAsErrors>true",
            "dotnet\.Source run"
        )

    Add-Check `
        "scripts\tests\LauncherCloudCancellationTest.cs" `
        "proves timeout, cancellation, disposal, repeated taps, legacy drain, and atomic-write cleanup" `
        @(
            "OnlyOneOperationCanRunAsync",
            "CancellationDrainsWorkerAsync",
            "TimeoutCancelsBeforeReturningAsync",
            "ExternalCancellationRemainsCancellationAsync",
            "DisposalCancelsAndDrainsAsync",
            "LegacySaveStoreIsDrainedAsync",
            "AtomicWriteReplacesAfterSuccessAsync",
            "CancelledAtomicWritePreservesDestinationAsync",
            "TimedOutAtomicWriteCannotMutateAfterReturnAsync",
            "mutated the destination after returning",
            "tests passed \{_passed\}/11"
        )
}
