function Add-SteamVersionSelectionBranchSelectorChecks {
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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.cs" `
        "declares launcher preference keys and typed preference storage roots" `
        @(
            "LocalBackupPreferenceKey",
            "CloudSyncPreferenceKey",
            "game_branch",
            "GameBranchPreference",
            "BooleanPreference",
            "OperatingSystem\.IsAndroid"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.GameBranch.cs" `
        "persists selected Steam game branch through normalized storage" `
        @(
            "ReadGameBranch",
            "SaveGameBranch",
            "GameBranchPreferenceExists",
            "SteamGameBranch\.Normalize",
            "SteamGameBranch\.Public",
            "GameBranchPreference\.WriteText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.ActionPreferences.cs" `
        "keeps aggregate launcher action preferences typed" `
        @(
            "internal readonly struct ActionPreferences",
            "LocalBackupEnabled",
            "CloudSyncEnabled",
            "GameBranch",
            "ReadActionPreferences",
            "LoadAndApplyActionPreferences"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.BooleanPreference.cs" `
        "isolates boolean preference read apply and save behavior" `
        @(
            "internal BooleanPreference",
            "Func<bool> defaultValue",
            "Action<bool> apply",
            "Action<bool>\? beforeSave = null",
            "LoadAndApply",
            "BeforeSave\?\.Invoke",
            "Storage\.WriteBoolean"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.LocalBackup.cs" `
        "keeps local-backup preference effects isolated from branch selection" `
        @(
            "SaveLocalBackupEnabled",
            "RequestStoragePermissionForLocalBackup",
            "AppPaths\.HasStoragePermission",
            "AppPaths\.RequestStoragePermission",
            "CloudSyncCoordinator\.SetLocalBackupEnabled",
            "AppPaths\.EnsureExternalDirectories"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.CloudSync.cs" `
        "keeps cloud-sync preference application isolated from branch selection" `
        @(
            "LoadAndApplyCloudSyncEnabled",
            "SaveCloudSyncEnabled",
            "ApplyCloudSync",
            "LauncherCloudSaveState\.SetCloudSyncEnabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
        "constructs selected-version selector controls before downloading" `
        @(
            "OptionButton",
            "BuildBranchDropdown",
            "ItemSelected"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.cs" `
        "keeps install branch selection and compact drawer mechanics isolated" `
        @(
            "SetGameBranch",
            "SetAvailableBranches",
            "UpdateBranchHelpText",
            "LauncherBranchDropdown\.Populate",
            "LauncherBranchDropdown\.NormalizeSelection",
            "selection\.Changed",
            "LauncherBranchDropdown\.TryGetBranch",
            "CollapseCompactBranchDetailsAfterSelection",
            "ApplyBranchControlVisibility",
            "_branchDetailsExpanded = true",
            "_branchDetailsExpanded = false",
            "GameBranchChanged\?\.Invoke"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.Text.cs" `
        "shows selector limitations before downloading a selected version" `
        @(
            "SelectedOptionStatus",
            "UpdateBranchHelpText",
            "SteamGameBranch\.SelectorInstallSlotHelpText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherBranchDropdown.cs" `
        "centralizes shared Steam branch dropdown population and safe selection lookup" `
        @(
            "internal static class LauncherBranchDropdown",
            "SelectionUpdate",
            "NormalizeSelection",
            "SteamGameBranch\.Normalize",
            "NormalizeAvailableBranches",
            "LauncherBranchCatalog\.DropdownOptions",
            "dropdown\.Clear\(\)",
            "branchOptions\.Clear\(\)",
            "dropdown\.AddItem\(option\.Label\)",
            "dropdown\.Select\(selectedIndex\)",
            "TryGetBranch",
            "index < 0 \|\| index >= branchOptions\.Count"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.cs" `
        "keeps ready/action branch selection and visibility mechanics isolated" `
        @(
            "SetGameBranch",
            "SetAvailableBranches",
            "UpdateBranchHelpText",
            "PopulateBranchDropdown",
            "LauncherBranchDropdown\.NormalizeSelection",
            "selection\.Changed",
            "LauncherBranchDropdown\.TryGetBranch",
            "LauncherBranchDropdown\.Populate",
            "CollapseCompactBranchDetailsAfterSelection",
            "_branchDetailsExpanded = false",
            "ApplyBranchControlVisibility",
            "GameBranchChanged\?\.Invoke"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
        "updates selector help text in ready/action state" `
        @(
            "_branchHelpLabel",
            "SelectedOptionStatus",
            "UpdateBranchHelpText",
            "SteamGameBranch\.SelectorInstallSlotHelpText",
            "Version/download actions affect local game files only",
            "Steam Cloud saves move only through Pull/Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Branch.cs" `
        "creates dropdown branch selector and wrapped selector help text in ready/action state" `
        @(
            "OptionButton",
            "ItemSelected",
            "ApplyGameBranch",
            "branchHelpLabel",
            "AutowrapMode",
            "MouseFilterEnum\.Ignore",
            "HorizontalAlignment\.Left"
        )
}
