function Add-SteamVersionSelectionCloudSafetyPushGateChecks {
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
}
