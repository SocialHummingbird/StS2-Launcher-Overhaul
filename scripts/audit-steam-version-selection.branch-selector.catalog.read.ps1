function Add-SteamVersionSelectionBranchSelectorCatalogReadChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.cs" `
        "reads account-visible Steam branch catalog and merges it with installed branch slots" `
        @(
            "SteamBranchAvailabilityMarkerFile\.Exists",
            "SteamBranchAvailabilityMarkerFile\.ReadVisibleRows",
            "BranchOptionFromMarkerRow",
            "ReadVisibleBranches",
            "ReadVisibleBranchNames",
            "ReadSelectableBranches",
            "SourceDescription",
            "Steam app-info visible branch catalog",
            "GroupBy"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.cs" `
        "uses the shared Steam branch availability marker file helper instead of direct file reads" `
        @(
            "BranchAvailabilityMarkerPath",
            "File\.ReadLines"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Installed.cs" `
        "discovers locally installed non-public branch slots without adding public duplicates" `
        @(
            "ReadInstalledBranches",
            "LauncherStorageNames\.GameVersionsDirectory",
            "LauncherBranchMarkerFields\.Branch",
            "SteamGameInstallPaths\.BranchMarkerFileName",
            "SteamGameBranch\.Normalize",
            "SteamGameBranch\.Public",
            "SteamGameBranch\.StateDirectoryName",
            "local install",
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "missingFileValue: string\.Empty",
            "missingLineValue: string\.Empty",
            "readFailedValue: string\.Empty"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Marker.cs" `
        "parses Steam app-info branch marker metadata into dropdown options" `
        @(
            "BranchOptionFromMarkerRow",
            "SteamBranchAvailabilityMarkerRow",
            "row\.MetadataVisible",
            "row\.WindowsManifestDepotCount",
            "row\.PasswordRequired",
            "row\.BuildId",
            "row\.Description",
            "Steam app-info"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Marker.cs" `
        "does not duplicate Steam branch availability visible-row parsing" `
        @(
            'IndexOf\(" \["',
            "ParseMetadata"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Merge.cs" `
        "keeps branch option replacement and fallback merge behavior isolated" `
        @(
            "AddIfMissing",
            "AddOrReplace",
            "StringComparison\.OrdinalIgnoreCase",
            "FindIndex",
            "options\[existingIndex\] = option"
        )
}
