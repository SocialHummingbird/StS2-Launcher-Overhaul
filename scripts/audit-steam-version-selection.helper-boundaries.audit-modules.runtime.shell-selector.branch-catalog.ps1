function Add-SteamVersionSelectionRuntimeBranchCatalogBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.catalog.ps1" `
        "keeps Steam branch catalog and dropdown option audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorCatalogChecks",
            "audit-steam-version-selection.branch-selector.catalog.capability.ps1",
            "audit-steam-version-selection.branch-selector.catalog.options.ps1",
            "audit-steam-version-selection.branch-selector.catalog.read.ps1",
            "audit-steam-version-selection.branch-selector.catalog.dropdown.ps1",
            "Add-SteamVersionSelectionBranchSelectorCatalogCapabilityChecks",
            "Add-SteamVersionSelectionBranchSelectorCatalogOptionChecks",
            "Add-SteamVersionSelectionBranchSelectorCatalogReadChecks",
            "Add-SteamVersionSelectionBranchSelectorCatalogDropdownChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.catalog.capability.ps1" `
        "keeps Steam branch selector capability and refresh audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorCatalogCapabilityChecks",
            "SteamGameBranch.cs",
            "DepotDownloader.UpdateCheck.cs",
            "LauncherController.BranchCatalog.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.catalog.options.ps1" `
        "keeps Steam branch option identity and status audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorCatalogOptionChecks",
            "LauncherBranchCatalog.Option.cs",
            "LauncherBranchCatalog.Option.Label.cs",
            "LauncherBranchCatalog.Option.Status.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.catalog.read.ps1" `
        "keeps Steam branch catalog marker read and merge audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorCatalogReadChecks",
            "LauncherBranchCatalog.Read.cs",
            "LauncherBranchCatalog.Read.Installed.cs",
            "LauncherBranchCatalog.Read.Marker.cs",
            "LauncherBranchCatalog.Read.Merge.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.catalog.dropdown.ps1" `
        "keeps Steam branch dropdown formatting and blocker audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorCatalogDropdownChecks",
            "LauncherBranchCatalog.Dropdown.cs",
            "LauncherBranchCatalog.Dropdown.Status.cs",
            "LauncherBranchCatalog.Dropdown.Metadata.cs"
        )
}
