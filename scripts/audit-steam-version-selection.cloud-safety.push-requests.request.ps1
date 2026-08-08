function Add-SteamVersionSelectionCloudSafetyPushRequestConstructionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.cs" `
        "keeps manual cloud sync request state and confirmation properties typed" `
        @(
            "CloudSyncTimeoutMs = 180_000",
            "private readonly partial struct ManualCloudSyncRequest",
            "ConfirmationMessage",
            "ConfirmText",
            "CancelText",
            "BypassConfirmation",
            "Func<CancellationToken, Task<ManualCloudSyncResult>> run",
            "Action<string, string>\? recordTerminalFailure = null",
            "Action\? onSuccessfulCompletion = null",
            "Action<Exception>\? onFailed = null",
            "CloudOperationProgressTracker\? operationProgress = null",
            "timeoutMs = CloudSyncTimeoutMs",
            "TimeoutMs = timeoutMs",
            "private int TimeoutMs",
            "OperationProgress = operationProgress",
            "private CloudOperationProgressTracker\? OperationProgress"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.cs" `
        "does not retain legacy completion-evidence or incomplete-result state" `
        @(
            "CompletionEvidenceRequired",
            "RecordCompletionEvidence",
            "RecordIncompleteResult",
            "ManualCloudSyncCompletion"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Factory.cs" `
        "captures one selected save context and routes both manual transfer directions through it" `
        @(
            "ManualCloudSyncRequest Push",
            "ManualCloudSyncRequest Pull",
            "PushConfirmationMessage\(dataDir, selectedBranch\)",
            "var saveNamespace = LauncherModSelectionState\.IsModdedMode",
            "LauncherModSelectionState\.EnabledModSetFingerprint\(\)",
            "SteamGameBranch\.StorageIdentity\(selectedBranch\)",
            "LauncherCloudSaveState\.ManualPushAllAsync",
            "LauncherCloudSaveState\.ManualPullAllAsync",
            "saveNamespace",
            "runtimeIdentity",
            "modSetFingerprint",
            "CloudOperationProgressTracker progress",
            "operationProgress: progress",
            "prepareOperation: \(\) =>",
            "EnsureCloudPushStillEligible",
            "EnsureSaveContextStillSelected",
            "WriteManualPushMarker",
            "WriteManualPushBlockedMarker",
            "WriteManualPullMarker",
            "Steam Cloud saves for .* to Android\?",
            "app-private backups are verified"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.PushConfirmation.cs" `
        "warns final Push confirmation about the selected context and account overwrite risk" `
        @(
            "Push Android local saves to Steam Cloud\?",
            "Selected game version:",
            "overwrite Steam Cloud saves for this Steam account",
            "selected game version and play mode match the local saves"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.PushConfirmation.cs" `
        "does not present obsolete Pull-first or optional-backup requirements as Push gates" `
        @(
            "Pull-after-switch",
            "Pull from Cloud first",
            "backup storage permission",
            "local pre-Push backup",
            "cloud pre-Push backup",
            "LauncherBranchSwitchSafety"
        )
}
