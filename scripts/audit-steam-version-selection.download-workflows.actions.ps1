function Add-SteamVersionSelectionDownloadWorkflowActionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDownloadCoordinator.Actions.cs" `
        "reports selected-version preservation when clearing inactive caches" `
        @(
            "SelectedOptionDownloadProblem",
            "blocked",
            "BlockedRedownloadConfirmationMessage",
            "ApplyRedownloadBlockedByBranchProblem",
            "replacement download remains blocked",
            "ClearCachedVersionsPressed",
            "ClearCachedVersions",
            "DeleteInactiveVersionCaches",
            "Selected version preserved",
            "runtime pack cache\(s\)",
            "SteamGameBranch\.DisplayName"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDownloadCoordinator.Execution.cs" `
        "logs selected-branch integrity summary after non-public downloads" `
        @(
            "BranchIntegritySummary",
            "integritySummary",
            "AppendLog"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDownloadCoordinator.Actions.cs" `
        "labels redownload and cache confirmations with explicit compact actions" `
        @(
            "Redownload Version",
            "Keep Files",
            "Delete Cache",
            "Clear Cache",
            "Keep Cache"
        )
}
