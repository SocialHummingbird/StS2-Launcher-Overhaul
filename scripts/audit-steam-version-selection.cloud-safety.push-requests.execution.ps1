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
            "OnSuccessfulCompletion",
            "completion diagnostic could not be recorded",
            "PatchHelper\.Log",
            "LauncherTimeout\.RunOrThrowAsync",
            "Task<ManualCloudSyncResult>",
            "TimeoutMs"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Lifecycle.cs" `
        "does not turn diagnostic marker failure into transfer failure or partial success" `
        @(
            "CompletionEvidenceRequired",
            "RecordCompletionEvidence",
            "RecordIncompleteResult",
            "ManualCloudSyncCompletion",
            "PartialSuccess"
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
        "models only live Pull phases and verified cumulative counts" `
        @(
            "CloudOperationKind",
            "CloudOperationPhase",
            "Preparing",
            "Enumerating",
            "BackingUp",
            "Transferring",
            "Finalizing",
            "Completed",
            "Failed",
            "EnumeratedPathCount",
            "BackupProcessedCount",
            "BackupCreatedCount",
            "TransferProcessedCount",
            "TransferCompletedCount",
            "IsActive",
            "IsTerminal"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudOperationState.cs" `
        "does not model transfer outcomes that the all-or-throw operation cannot return" `
        @(
            "PreparingProfiles",
            "SeedingProfiles",
            "PartiallyCompleted",
            "CloudTransferPathOutcome",
            "TransferSkippedCount",
            "TransferFailedCount",
            "TransferTimedOutCount",
            "ProfileSeed"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudOperationProgressTracker.cs" `
        "publishes immutable snapshots for live Pull phases and verified count updates" `
        @(
            "CloudOperationProgressTracker",
            "Action<CloudOperationState>",
            "EnumerationStarted",
            "EnumerationCompleted",
            "BackupStarted",
            "BackupPathStarted",
            "BackupProcessed",
            "TransferStarted",
            "TransferPathStarted",
            "TransferProcessed",
            "Finalizing",
            "Completed",
            "Failed",
            "_publish\?\.Invoke\(_state\)"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudOperationProgressTracker.cs" `
        "does not retain skipped, partial, profile-preparation, or profile-seeding branches" `
        @(
            "CloudTransferPathOutcome",
            "PartiallyCompleted",
            "ProfilePreparation",
            "ProfileSeed",
            "TransferSkipped",
            "TransferFailed",
            "TransferTimedOut"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs" `
        "routes context-required Push and Pull entry points to one direction-parameterized sync operation" `
        @(
            "ManualPushAllAsync",
            "ManualPullAllAsync",
            "SaveNamespace saveNamespace",
            "string runtimeIdentity",
            "string modSetFingerprint",
            "CloudOperationKind\.Push",
            "CloudOperationKind\.Pull",
            "RunManualSyncAsync"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs" `
        "does not restore contextless defaults or direction-specific transfer implementations" `
        @(
            "SaveNamespace saveNamespace = SaveNamespace\.Vanilla",
            "RunManualPush",
            "RunManualPull",
            "ContentTransfer",
            "ModdedSaveSeed"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs" `
        "owns authentication setup and reports only verified transfer completion or failure" `
        @(
            "progress\.Preparing",
            "new ManualSyncContext",
            "RunTransferAsync",
            "progress\.Completed",
            "progress\.Failed",
            "throw;"
        )

    Add-Check `
        "src\STS2Mobile\Steam\ManualCloudSyncResult.cs" `
        "reports only data from a fully verified transfer" `
        @(
            "CloudOperationKind Kind",
            "int TransferredPathCount",
            "int BackupCreatedCount",
            "string Detail"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\ManualCloudSyncResult.cs" `
        "does not represent partial, skipped, failed, timed-out, or post-processing outcomes" `
        @(
            "ManualCloudSyncCompletion",
            "CandidatePathCount",
            "CompletedPathCount",
            "SkippedPathCount",
            "FailedPathCount",
            "TimedOutPathCount",
            "UnprocessedPathCount",
            "PrivateBackup",
            "ProfileSeed",
            "PostProcessing",
            "FaultCount",
            "CanRecordCompletionEvidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudOperationTerminalPresentation.cs" `
        "reports verified completion as success and incomplete work only through thrown terminal outcomes" `
        @(
            "CloudOperationTerminalOutcome\.Success",
            "CreateCompletion",
            "Pull succeeded",
            "Upload succeeded",
            "CreateTimeout",
            "CreateFailure",
            "CreateCancelled",
            "TransferredPathCount"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\CloudOperationTerminalPresentation.cs" `
        "does not claim partial transfer success or gate Upload on legacy evidence" `
        @(
            "PartialSuccess",
            "ManualCloudSyncCompletion",
            "CompletionEvidence",
            "Upload remains locked"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs" `
        "implements Push and Pull once from an immutable snapshot with mandatory backup and verification" `
        @(
            "RunTransferAsync",
            "CloudOperationKind direction",
            "SaveContext\.Create",
            "CaptureSourceSnapshotAsync",
            "BackupDestinationsAsync",
            "foreach \(var item in snapshot\)",
            "WriteDestinationFileBytesAsync",
            "ReadDestinationFileBytesAsync",
            "DeleteDestinationFileAsync",
            "RequireHash",
            "WriteAndVerifyRemoteAsync",
            "WriteAndVerifyLocalAsync"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SaveContext.cs" `
        "binds transfers to the authenticated account, namespace, runtime identity, and exact mod set" `
        @(
            "ulong SteamId64",
            "SaveNamespace Namespace",
            "string RuntimeIdentity",
            "string ModSetFingerprint",
            "SteamID64 must be authenticated",
            "Vanilla saves cannot carry a mod-set fingerprint",
            "Modded saves require a mod-set fingerprint",
            "RequireExactMatch",
            "Steam account",
            "save namespace",
            "runtime compatibility/branch",
            "mod set"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SaveTransferAllowlist.cs" `
        "defines explicit namespace-aware save paths while leaving device settings out" `
        @(
            'SharedProfilePath = "profile\.save"',
            'ProgressFile = "progress\.save"',
            'PreferencesFile = "prefs\.save"',
            'CurrentRunFile = "current_run\.save"',
            'CurrentMultiplayerRunFile = "current_run_mp\.save"',
            'saveNamespace == SaveNamespace\.Modded \? "modded/" : ""',
            'HistoryDirectory',
            'RunHistoryLimit = 100',
            'IsAllowedPath',
            'IsAllowedHistoryFilename'
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\SaveTransferAllowlist.cs" `
        "does not broaden transfer scope to device settings or invented modded root metadata" `
        @(
            "settings",
            'modded/profile\.save',
            "SavePathDiscovery"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudOperationPresentation.cs" `
        "turns every Pull state into visible current-phase, count, item, and completion text" `
        @(
            "CloudOperationPresentation",
            "Finding Steam Cloud saves",
            "Backing up Android saves",
            "Downloading Steam Cloud saves",
            'Finishing \{operation\}',
            '\{operation\} complete',
            '\{operation\} stopped',
            "TransferProcessedCount",
            "TransferCompletedCount",
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
            "tracker\.Completed"
        )

    Add-Check `
        "tools\LauncherUiPreview\LauncherCloudProgressPreviewValidator.cs" `
        "checks rendered Pull phase, verified counts, current item, and progress" `
        @(
            "Downloading Steam Cloud saves",
            "11/24 checked",
            "11 verified",
            "Current: profile2/saves/progress\.save",
            "Pull complete",
            "24/24 checked",
            "24 verified",
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
        "threads one owned token through direction-neutral source, destination, and verification operations" `
        @(
            "ITransferSaveStore _cloudTransfer",
            "CancellationToken _cancellationToken",
            "GetAuthenticatedSteamId64Async",
            "metadata\.PrepareFileMetadata\(_cancellationToken\)",
            "GetSourceHistoryFiles",
            "SourceFileExistsAsync",
            "ReadSourceFileBytesAsync",
            "DestinationFileExistsAsync",
            "ReadDestinationFileBytesAsync",
            "WriteDestinationFileBytesAsync",
            "DeleteDestinationFileAsync",
            "ReadFileBytesForVerificationAsync",
            "FileExistsForVerificationAsync",
            "DeleteFileAsync",
            "CancellableSaveStore\.ReadBytesAsync",
            "CancellableSaveStore\.WriteFileAsync",
            "WaitForCloudOperationAsync",
            "_cancellationToken"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Context.cs" `
        "does not expose synchronous manual reads or queued immediate-upload batches" `
        @(
            "internal string\? ReadLocalFile",
            "RunCloudBatchImmediate",
            "EndSaveBatchAndUploadNow",
            "FlushCloudWrites",
            "SavePathDiscovery",
            "RefreshLocalMirror"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs" `
        "awaits each transfer mutation and verifies the resulting destination state" `
        @(
            "sync\.Checkpoint\(\)",
            "await sync\.WriteDestinationFileBytesAsync",
            "await sync\.ReadDestinationFileBytesAsync",
            "await sync\.DeleteDestinationFileAsync",
            "await sync\.DestinationFileExistsAsync",
            "RequireHash\(",
            "direction == CloudOperationKind\.Push",
            '"Steam Cloud"',
            '"Android local storage"',
            "ReportTransferProcessed"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs" `
        "cannot enqueue hidden uploads, seed across namespaces, or refresh an optional mirror after returning" `
        @(
            "RunCloudBatchImmediate",
            "EndSaveBatchAndUploadNow",
            "EnqueueUpload",
            "CancellationToken\.None",
            "ModdedSaveSeed",
            "RefreshLocalMirror"
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
            "job\.Timeout = TimeSpan\.FromMilliseconds\(CloudRpcTimeoutMs\)",
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
