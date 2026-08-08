function Add-SteamVersionSelectionCloudSafetyBranchSwitchGateChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "validates branch-switch marker history and selected-branch identity" `
        @(
            "HasRequiredEvidence",
            "SelectedBranchMatches",
            "SteamGameBranch\.Normalize"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "does not treat Pull, save-origin, branch history, or storage as Push eligibility" `
        @(
            "ManualPushPrerequisitesSatisfied",
            "HasManualPullAfterBranchSwitch",
            "CurrentLocalSavesMatchSelectedRuntime",
            "AppPaths\.HasStoragePermission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
        "writes branch-switch safety marker and marks save-origin pending" `
        @(
            "WriteMarker",
            "LocalBackupForcedPrefix",
            "WarningAcknowledgedPrefix",
            "NonPublicBranchWarningAcknowledgedPrefix",
            "SelectedBranchSelectionKindPrefix",
            "SelectorModePrefix",
            "SelectedBranchNotePrefix",
            "SelectedVersionPrefix",
            "SelectedVersionSlotKindPrefix",
            "SelectedVersionSlotDirectoryPrefix",
            "SelectorHelpText",
            "WriteBranchSwitchPendingOrigin",
            "beta password entry is not implemented",
            "Failed to write branch switch safety marker"
        )
}
