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
            "Completed\(bool hasUpdate\)",
            "UpdateGameFilesButtonText",
            "Update available for selected game version",
            "Selected game version is up to date",
            "Failed\(string message\)",
            "UpdateCheckFailedButtonText",
            "Blocked\(string message\)",
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
            "SetUpdateCheckBusy\(busy: true\)",
            "CheckForUpdatesAsync",
            "PatchHelper\.Log",
            "_versions\.FailUpdateCheck\(ex\.Message\)",
            "SetUpdateCheckBusy\(busy: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUpdateCoordinator.Workflow.cs" `
        "blocks selected-version update checks for known unavailable branches while preserving app update checks" `
        @(
            "CheckForAppUpdatesAsync",
            "SelectedOptionDownloadProblem",
            "Update check blocked:",
            "LauncherBranchCatalog\.ReadVisibleBranches",
            "_model\.CheckForUpdatesAsync",
            "await appUpdateTask"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherVersionCoordinator.UpdateChecks.Results.cs" `
        "applies update-check completion and failure events after refreshing branch options" `
        @(
            "CompleteUpdateCheck",
            "FailUpdateCheck",
            "RefreshGameBranchOptions",
            "UpdateCheckViewUpdate\.Completed",
            "UpdateCheckViewUpdate\.Failed",
            "LauncherBranchAvailabilityStatus\.CompactFailureMessage"
        )
}
