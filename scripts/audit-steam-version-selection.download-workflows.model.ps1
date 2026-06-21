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

}
