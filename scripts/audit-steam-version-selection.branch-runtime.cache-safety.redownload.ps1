function Add-SteamVersionSelectionBranchRuntimeRedownloadCleanupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.cs" `
        "orchestrates selected-version redownload cleanup without touching other branch caches" `
        @(
            "DeleteDownloadedState",
            "SteamGameBranch\.Normalize",
            "GameDirectoryPath\(dataDir, branch\)",
            "SteamGameInstallPaths\.DownloadStateDirectoryPath\(dataDir, branch\)",
            "GameRuntimeSlot\.RuntimePackDirectoryPath\(dataDir, branch\)",
            "WriteRedownloadMarker",
            "DeleteDirectory\(runtimePackDirectory\)",
            "LauncherRuntimeSlotEvidence\.Clear\(dataDir\)",
            "LauncherRuntimeCacheEvidence\.Clear\(dataDir\)",
            "LauncherRuntimePatchValidationEvidence\.Clear\(dataDir\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Fields.cs" `
        "centralizes selected-version redownload marker prefixes" `
        @(
            "RedownloadMarkerUtcPrefix = ""UTC:""",
            "RedownloadMarkerSelectedBranchPrefix = ""Selected branch:""",
            "RedownloadMarkerVersionSlotKindPrefix = ""Selected version slot kind:""",
            "RedownloadMarkerVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "RedownloadMarkerGameDirectoryPrefix = ""Deleted game directory:""",
            "RedownloadMarkerGameDirectoryExistedPrefix = ""Game directory existed before delete:""",
            "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix = ""Game directory exists after delete:""",
            "RedownloadMarkerDownloadStateDirectoryPrefix = ""Deleted download state directory:""",
            "RedownloadMarkerDownloadStateDirectoryExistedPrefix = ""Download state directory existed before delete:""",
            "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix = ""Download state directory exists after delete:""",
            "RedownloadMarkerRuntimePackDirectoryPrefix = ""Deleted runtime pack directory:""",
            "RedownloadMarkerRuntimePackDirectoryExistedPrefix = ""Runtime pack directory existed before delete:""",
            "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix = ""Runtime pack directory exists after delete:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Read.cs" `
        "reads selected-version redownload cleanup marker evidence" `
        @(
            "RedownloadMarkerFileName",
            "RedownloadMarkerUtcParseable",
            "RedownloadMarkerVersionSlotKind",
            "RedownloadMarkerVersionSlotDirectory",
            "last_game_version_redownload\.txt",
            "RedownloadMarkerRuntimePackDirectory",
            "RedownloadMarkerSelectedDirectoriesCleared"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Write.cs" `
        "writes selected-version redownload cleanup marker evidence" `
        @(
            "WriteRedownloadMarker",
            "RedownloadMarkerSelectedBranchPrefix",
            "RedownloadMarkerVersionSlotKindPrefix",
            "RedownloadMarkerVersionSlotDirectoryPrefix",
            "RedownloadMarkerGameDirectoryPrefix",
            "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix",
            "RedownloadMarkerDownloadStateDirectoryPrefix",
            "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix",
            "RedownloadMarkerRuntimePackDirectoryPrefix",
            "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix",
            "File\.WriteAllLines",
            "Failed to write game version redownload marker"
        )
}
