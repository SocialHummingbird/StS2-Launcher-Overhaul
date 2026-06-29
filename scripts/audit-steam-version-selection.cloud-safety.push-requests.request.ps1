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
            "Func<Task<string>> run",
            "Action<Exception>\? onFailed = null"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.Factory.cs" `
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
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.Request.PushConfirmation.cs" `
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
}
