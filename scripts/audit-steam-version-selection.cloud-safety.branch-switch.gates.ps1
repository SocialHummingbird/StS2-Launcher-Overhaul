function Add-SteamVersionSelectionCloudSafetyBranchSwitchGateChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "enforces branch-switch safety gates before manual Push" `
        @(
            "HasRequiredEvidence",
            "SelectedBranchMatches",
            "ManualPushPrerequisitesSatisfied",
            "SteamGameBranch\.Normalize",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "AppPaths\.HasStoragePermission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
        "writes branch-switch safety marker and marks save-origin pending" `
        @(
            "WriteMarker",
            "LocalBackupForcedPrefix",
            "ManualPushRequiresBackupStoragePrefix",
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
