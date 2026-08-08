function Add-SteamVersionSelectionBranchSelectorStoragePathChecks {
    Add-Check `
        "src\STS2Mobile\Steam\SteamGameInstallPaths.cs" `
        "keeps public and non-public install/download state separated" `
        @(
            "game_versions",
            "download_state",
            "steam_branch\.txt",
            "last_steam_branch_availability\.txt",
            "BranchAvailabilityMarkerPath",
            "VersionSlotDirectory",
            "VersionSlotKind",
            "public legacy",
            "side-by-side branch cache",
            "BranchMarkerPath"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.RuntimeFiles.cs" `
        "keeps managed startup branch cache routing aligned with downloader paths" `
        @(
            "GameVersionsDirectoryName",
            "StateDirectoryName",
            "StorageIdentity",
            "StableBranchHash",
            "safePrefix",
            "TrimEnd",
            "StableBranchHash\(branch\)"
        )
}
