function Add-SteamVersionSelectionBranchSelectorCatalogDropdownChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.cs" `
        "builds branch dropdown options without treating gated branches as available" `
        @(
            "DropdownOptions",
            "DropdownOptionLabels",
            "new\(SteamGameBranch\.Public, source: ""fallback""\)",
            "AddOrReplace\(options, branch\)",
            "AddIfMissing\(options, new BranchOption\(selectedBranch, source: ""saved selection""\)\)",
            "SteamGameBranch\.Public"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.Status.cs" `
        "keeps selected branch dropdown status and download blockers isolated" `
        @(
            "SelectedOptionStatus",
            "SelectedOptionCompactStatus",
            "SelectedOptionDownloadProblem",
            "Password branch blocked",
            "Ready in Steam catalog",
            "Refresh before download",
            "saved selection",
            "not listed in the latest Steam app-info catalog",
            "private, inaccessible, password-protected, or unavailable",
            "Download blocked: selected saved branch was not listed",
            "Download blocked: selected branch is password-protected",
            "Download blocked: selected branch has no Windows depot manifest",
            "SteamGameBranch\.Public"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.Metadata.cs" `
        "keeps branch dropdown diagnostics metadata formatting isolated" `
        @(
            "DropdownOptionMetadata",
            "DropdownOptions\(selectedBranch, discoveredBranches\)",
            "SteamBranchAvailabilityMarkerFields\.MetadataVisibleKey",
            "SteamBranchAvailabilityMarkerFields\.WindowsManifestDepotsKey",
            "SteamBranchAvailabilityMarkerFields\.PasswordRequiredKey",
            "SteamBranchAvailabilityMarkerFields\.BuildIdKey",
            "ValueOrUnknown",
            "ValueOrNone"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.cs" `
        "does not inject hardcoded beta as a normal dropdown fallback" `
        @(
            "SteamGameBranch\.Beta,\s*source:\s*""fallback""",
            "AddIfMissing\(options,\s*new BranchOption\(SteamGameBranch\.Beta"
        )
}
