function Add-SteamVersionSelectionDownloadWorkflowModelChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.cs" `
        "keeps launcher download model state and events centralized" `
        @(
            "_downloadCts",
            "_downloader",
            "_downloadRunning",
            "DownloadProgressChanged",
            "DownloadLogReceived",
            "Action<string> DownloadCompleted",
            "Action<LauncherBranchOperationFailure> DownloadFailed",
            "Action<string> DownloadCancelled",
            "Action<LauncherUpdateCheckResult> UpdateCheckCompleted",
            "Action<LauncherBranchOperationFailure> UpdateCheckFailed",
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
            "Download\(LauncherModel model, string branch\)",
            "BeginDownload\(connection, branch\)",
            "RunDownloadAsync\(branch\)",
            "UpdateCheck\(LauncherModel model, string branch\)",
            "CheckForUpdatesWithConnectionAsync\(connection, branch\)",
            "RaiseDownloadFailed\(branch, message\)",
            "RaiseUpdateCheckFailed\(branch, message\)",
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
            "SteamGameBranch\.Normalize\(branch\)",
            "DownloadRunGuard\.TryAcquire\(this\)",
            "Download already running",
            "LauncherLaunchReadinessCache\.Clear\(""download model started""\)",
            "DepotConnectionAction\.Download\(this, branch\)",
            "run\.Release\(\)",
            "CheckForUpdatesAsync",
            "DepotConnectionAction\.UpdateCheck\(this, branch\)",
            "RefreshBranchCatalogAsync",
            "DepotConnectionAction\.BranchCatalogRefresh\(this\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Downloads.Catalog.cs" `
        "keeps update checks and branch catalog refreshes as non-download depot workflows" `
        @(
            "CheckForUpdatesWithConnectionAsync",
            "using var downloader = CreateDownloader\(connection, branch\)",
            "CheckForUpdatesAsync",
            "RaiseUpdateCheckCompleted\(branch, hasUpdate\)",
            "RaiseUpdateCheckFailed\(branch, ex\.Message\)",
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

}
