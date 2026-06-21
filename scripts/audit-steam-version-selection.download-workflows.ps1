function Add-SteamVersionSelectionDownloadWorkflowChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.cs" `
        "keeps launcher download model state and events centralized" `
        @(
            "_downloadCts",
            "_downloader",
            "_downloadRunning",
            "DownloadProgressChanged",
            "DownloadLogReceived",
            "DownloadCompleted",
            "DownloadFailed",
            "DownloadCancelled",
            "UpdateCheckCompleted",
            "UpdateCheckFailed",
            "BranchCatalogRefreshCompleted",
            "BranchCatalogRefreshFailed",
            "DownloadIsRunning",
            "Interlocked\.CompareExchange"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.Action.cs" `
        "centralizes depot connection actions for download, update check, and branch refresh" `
        @(
            "NotConnectedMessage = ""Not connected""",
            "DepotConnectionAction",
            "Download\(LauncherModel model\)",
            "BeginDownload\(connection\)",
            "RunDownloadAsync\(\)",
            "UpdateCheck\(LauncherModel model\)",
            "CheckForUpdatesWithConnectionAsync",
            "BranchCatalogRefresh\(LauncherModel model\)",
            "RefreshBranchCatalogWithConnectionAsync",
            "FailNotConnected"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.RunGuard.cs" `
        "isolates download concurrency guard acquisition and release" `
        @(
            "DownloadRunGuard",
            "TryAcquire\(LauncherModel model\)",
            "Interlocked\.Exchange\(ref model\._downloadRunning, 1\) == 0",
            "Release\(\)",
            "Interlocked\.Exchange\(ref Model\._downloadRunning, 0\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.Start.cs" `
        "routes public download, update check, and branch refresh starts through depot connection actions" `
        @(
            "StartDownloadAsync",
            "DownloadRunGuard\.TryAcquire\(this\)",
            "Download already running",
            "DepotConnectionAction\.Download\(this\)",
            "run\.Release\(\)",
            "CheckForUpdatesAsync",
            "DepotConnectionAction\.UpdateCheck\(this\)",
            "RefreshBranchCatalogAsync",
            "DepotConnectionAction\.BranchCatalogRefresh\(this\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.Catalog.cs" `
        "keeps update checks and branch catalog refreshes as non-download depot workflows" `
        @(
            "CheckForUpdatesWithConnectionAsync",
            "using var downloader = CreateDownloader\(connection\)",
            "CheckForUpdatesAsync",
            "RaiseUpdateCheckCompleted",
            "RaiseUpdateCheckFailed",
            "RefreshBranchCatalogWithConnectionAsync",
            "RefreshBranchCatalogAsync",
            "RaiseBranchCatalogRefreshCompleted",
            "RaiseBranchCatalogRefreshFailed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.Connection.cs" `
        "keeps depot connection validation before download-related operations" `
        @(
            "RunWithDepotConnectionAsync",
            "GetDepotConnectionAsync",
            "EnsureConnectedAsync",
            "_steamSession\.TryGetConnection",
            "action\.FailNotConnected",
            "action\.RunAsync\(connection\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Downloads.Actions.cs" `
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
        "src\STS2Mobile\Launcher\LauncherController.Downloads.Execution.cs" `
        "logs selected-branch integrity summary after non-public downloads" `
        @(
            "BranchIntegritySummary",
            "integritySummary",
            "AppendLog"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Downloads.Actions.cs" `
        "labels redownload and cache confirmations with explicit compact actions" `
        @(
            "Redownload Version",
            "Keep Files",
            "Delete Cache",
            "Clear Cache",
            "Keep Cache"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.cs" `
        "keeps update-check button labels and running state centralized" `
        @(
            "UpdateCheckFailedButtonText = ""Check Failed""",
            "UpdateCheckBlockedButtonText = ""Check Blocked""",
            "UpToDateButtonText = ""Up to Date""",
            "UpdateGameFilesButtonText = ""Update Selected Version""",
            "_updateCheckRunning"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.ViewUpdate.cs" `
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
        "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Run.cs" `
        "runs selected-version update checks with busy-state and failure recovery" `
        @(
            "RunUpdateCheck",
            "RunUpdateCheckAsync",
            "_updateCheckRunning",
            "SetUpdateCheckBusy\(busy: true\)",
            "CheckForUpdatesAsync",
            "PatchHelper\.Log",
            "FailUpdateCheck\(ex\.Message\)",
            "SetUpdateCheckBusy\(busy: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Workflow.cs" `
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
        "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Results.cs" `
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
