function Add-SteamVersionSelectionDownloadWorkflowUpdateCheckChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUpdateCoordinator.cs" `
        "keeps update-check running state centralized in the update coordinator" `
        @(
            "_updateCheckRunning"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\UpdateCheckViewUpdate.cs" `
        "formats update-check view changes without running update logic" `
        @(
            "UpdateCheckViewUpdate",
            "Completed\(bool hasUpdate, string selectedVersion\)",
            "UpdateGameFilesButtonText",
            "Update available for selected game version",
            "Selected game version is up to date",
            "Failed\(string message, string selectedVersion\)",
            "UpdateCheckFailedButtonText",
            "Blocked\(string message, string selectedVersion\)",
            "UpdateCheckBlockedButtonText",
            "Check Blocked",
            "Update check blocked for selected game version",
            "view\.AppendLog",
            "view\.HideActions",
            "view\.ShowDownloadAction",
            "view\.SetStatus",
            "view\.SetUpdateButtonText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUpdateCoordinator.Run.cs" `
        "runs selected-version update checks with busy-state and failure recovery" `
        @(
            "RunUpdateCheck",
            "RunUpdateCheckAsync",
            "_updateCheckRunning",
            "var branch = LauncherPreferences\.ReadGameBranch\(\)",
            "SetUpdateCheckBusy\(busy: true\)",
            "CheckForUpdatesAsync\(branch\)",
            "PatchHelper\.Log",
            "new LauncherBranchOperationFailure\(branch, ex\.Message\)",
            "SetUpdateCheckBusy\(busy: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUpdateCoordinator.Workflow.cs" `
        "blocks selected-version update checks for known unavailable branches while preserving app update checks" `
        @(
            "CheckForAppUpdatesAsync",
            "CheckForUpdatesAsync\(string selectedBranch\)",
            "SelectedOptionDownloadProblem",
            "Update check blocked:",
            "SteamGameBranch\.DisplayName\(selectedBranch\)",
            "LauncherBranchCatalog\.ReadVisibleBranches",
            "_model\.CheckForUpdatesAsync\(selectedBranch\)",
            "await appUpdateTask"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherVersionCoordinator.UpdateChecks.Results.cs" `
        "applies update-check completion and failure events after refreshing branch options" `
        @(
            "CompleteUpdateCheck",
            "FailUpdateCheck",
            "LauncherUpdateCheckResult result",
            "LauncherBranchOperationFailure failure",
            "RefreshGameBranchOptions",
            "UpdateCheckViewUpdate\.Completed",
            "UpdateCheckViewUpdate\.Failed",
            "SteamGameBranch\.DisplayName\(result\.Branch\)",
            "SteamGameBranch\.DisplayName\(failure\.Branch\)",
            "LauncherBranchAvailabilityStatus\.CompactFailureMessage"
        )
}
