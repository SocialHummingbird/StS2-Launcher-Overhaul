function Add-SteamVersionSelectionBranchSelectorCatalogCapabilityChecks {
    Add-Check `
        "src\STS2Mobile\Steam\SteamGameBranch.cs" `
        "declares current selector capabilities and unsupported beta features" `
        @(
            "Public\s*=\s*""public""",
            "Beta\s*=\s*""beta""",
            "SelectorMode\s*=\s*""Steam branch dropdown""",
            "SelectionKind",
            "SelectorHelpText",
            "SelectorInstallSlotHelpText",
            "Active install slot",
            "Choose a game version from the dropdown",
            "Private/password-protected branches may be inaccessible",
            "Failed downloads do not change Steam Cloud saves",
            "BetaPasswordEntrySupported\s*=\s*false",
            "BranchDiscoverySupported\s*=\s*true",
            "Account-visible branch options refresh after Steam app-info is available",
            "StorageIdentity",
            "StableBranchHash",
            "safePrefix",
            "TrimEnd",
            "StableBranchHash\(storageBranch\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.UpdateCheck.cs" `
        "refreshes Steam branch catalog without starting a download" `
        @(
            "RefreshBranchCatalogAsync",
            "Refreshing Steam branch catalog",
            "GetMainAppDepotsAsync",
            "Steam branch catalog refreshed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.BranchCatalog.cs" `
        "wires non-mutating refresh game versions action" `
        @(
            "RunBranchCatalogRefresh",
            "RefreshBranchCatalogAsync",
            "This does not download or modify game files",
            "CompleteBranchCatalogRefresh",
            "FailBranchCatalogRefresh",
            "SetRefreshGameVersionsBusy",
            "SelectedOptionStatus",
            "SelectedOptionDownloadProblem",
            "Selected version:",
            "RefreshGameBranchOptions"
        )
}
