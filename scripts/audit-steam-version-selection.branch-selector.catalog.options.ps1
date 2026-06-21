function Add-SteamVersionSelectionBranchSelectorCatalogOptionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.cs" `
        "describes selectable Steam branch option identity and metadata fields" `
        @(
            "internal readonly partial struct BranchOption",
            "SteamGameBranch\.Normalize",
            "MetadataVisible",
            "WindowsManifestDepotCount",
            "PasswordRequired",
            "BuildId",
            "Description",
            "Source"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.Label.cs" `
        "keeps selectable Steam branch dropdown label formatting isolated" `
        @(
            "Label",
            "DropdownLabelWithMetadata",
            "SteamGameBranch\.DropdownLabel",
            "\(installed\)",
            "\(password\)",
            "\(unavailable\)",
            "\(ready\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.Status.cs" `
        "keeps selectable Steam branch status text and download blockers isolated" `
        @(
            "StatusText",
            "Download blocked: Steam marks this branch as password-protected",
            "password gate still blocks this launcher from downloading it",
            "Download blocked: this branch is visible to this account, but no Windows depot manifest was exposed",
            "Download blocked: this branch was not listed in Steam branch metadata",
            "Steam app-info metadata has not been captured",
            "SteamGameBranch\.Public"
        )
}
