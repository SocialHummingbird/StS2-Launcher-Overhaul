function Add-SteamVersionSelectionCloudSafetyPushRequestChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.cs" `
        "keeps manual Push entry points routed through shared safety gates" `
        @(
            "CloudPushPressed",
            "CanArmCloudPush",
            "CloudPushSafetyContext\.Create",
            "CanPushWithBaselineEvidence\(pushContext\)",
            "CanPushAfterBranchSwitch\(pushContext\)",
            "ManualCloudSyncRequest\.Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Context.cs" `
        "captures selected branch context for Push safety markers" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SteamGameBranch\.DisplayName",
            "SelectedBranch",
            "SelectedVersion",
            "WriteBlockedMarker",
            "WriteManualPushBlockedMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Baseline.cs" `
        "guards baseline manual Push until Pull, local save, and save-origin evidence match" `
        @(
            "CanPushWithBaselineEvidence",
            "Pull from Cloud must complete for the selected game version before Push",
            "selected game version \{pushContext\.SelectedVersion\}",
            "no Android local save evidence exists before Push",
            "LastManualPullCompletionRecorded",
            "LastManualPullMatchesSelectedBranch",
            "WriteBlockedMarker",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: Android local save origin evidence does not match the selected runtime"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.BranchSwitch.cs" `
        "guards manual Push after branch switches until backup storage is available" `
        @(
            "CanPushAfterBranchSwitch",
            "BranchSwitchSafety",
            "LauncherBranchSwitchSafety\.HasRequiredEvidence",
            "branch switch marker is missing required safety evidence",
            "does not match the selected game version",
            "no current Pull-after-switch evidence exists",
            "no Android local save evidence exists",
            "backup storage permission is unavailable",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "WriteBlockedMarker",
            "Pull from Cloud must complete after this game-version switch before Push",
            "HasManualPullAfterBranchSwitch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "no Android local save files were found",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch",
            "backup storage",
            "Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.cs" `
        "keeps manual cloud sync request state and confirmation properties typed" `
        @(
            "CloudSyncTimeoutMs = 180_000",
            "private readonly partial struct ManualCloudSyncRequest",
            "ConfirmationMessage",
            "ConfirmText",
            "CancelText",
            "BypassConfirmation",
            "Func<Task<string>> run",
            "Action<Exception>\? onFailed = null"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Factory.cs" `
        "routes manual Pull and Push requests through explicit markers and request callbacks" `
        @(
            "ManualCloudSyncRequest Push",
            "ManualCloudSyncRequest Pull",
            "PushConfirmationMessage\(dataDir, selectedBranch\)",
            "LauncherCloudSaveState\.ManualPushAllAsync",
            "LauncherCloudSaveState\.ManualPullAllAsync",
            "WriteManualPushMarker",
            "WriteManualPushBlockedMarker",
            "WriteManualPullMarker",
            "Pull Steam Cloud saves to Android local storage"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.PushConfirmation.cs" `
        "warns final Push confirmation about selected version, branch switches, and save overwrite risk" `
        @(
            "Selected version slot:",
            "Pull-after-switch for",
            "Android local save evidence",
            "local pre-Push backup",
            "cloud pre-Push backup",
            "A game version switch was recorded",
            "cross-version/destructive",
            "LauncherBranchSwitchSafety\.HasMarker",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Pull from Cloud first and verify the Android saves exist before pushing"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Lifecycle.cs" `
        "keeps manual cloud sync lifecycle UI updates and timeout execution isolated" `
        @(
            "ShowStarted",
            "ShowComplete",
            "ShowFailed",
            "ShowFinished",
            "RunWithTimeoutAsync",
            "SetPushPullDisabled\(true\)",
            "SetPushPullDisabled\(false\)",
            "Open Help & Reports for details",
            "PatchHelper\.Log",
            "LauncherTimeout\.RunOrThrowAsync",
            "CloudSyncTimeoutMs"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Execution.cs" `
        "executes Pull/Push cloud sync requests without bypassing Push confirmation" `
        @(
            "ManualCloudSyncRequest\.Pull\(",
            "RequestCloudSync",
            "ExecuteCloudSyncAsync",
            "request\.BypassConfirmation",
            "ShowConfirmation",
            "RunOnMainThread"
        )

}
