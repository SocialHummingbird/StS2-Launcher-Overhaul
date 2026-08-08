function Add-SteamVersionSelectionRuntimeBranchSelectorBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.ps1" `
        "keeps Steam branch selector audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorChecks",
            "audit-steam-version-selection.branch-selector.catalog.ps1",
            "audit-steam-version-selection.branch-selector.storage.ps1",
            "audit-steam-version-selection.branch-selector.download-section.ps1",
            "audit-steam-version-selection.branch-selector.action-section.ps1",
            "Add-SteamVersionSelectionBranchSelectorCatalogChecks",
            "Add-SteamVersionSelectionBranchSelectorStorageChecks",
            "Add-SteamVersionSelectionBranchSelectorDownloadSectionChecks",
            "Add-SteamVersionSelectionBranchSelectorActionSectionChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.storage.ps1" `
        "keeps branch selector storage audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorStorageChecks",
            "audit-steam-version-selection.branch-selector.storage.paths.ps1",
            "audit-steam-version-selection.branch-selector.storage.preferences.ps1",
            "audit-steam-version-selection.branch-selector.storage.cloud.ps1",
            "Add-SteamVersionSelectionBranchSelectorStoragePathChecks",
            "Add-SteamVersionSelectionBranchSelectorStoragePreferenceChecks",
            "Add-SteamVersionSelectionBranchSelectorStorageCloudPreferenceChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.storage.paths.ps1" `
        "keeps branch selector install-path and runtime cache audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorStoragePathChecks",
            "SteamGameInstallPaths.cs",
            "ModEntry.RuntimeFiles.cs",
            "VersionSlotDirectory",
            "BranchMarkerPath",
            "StableBranchHash"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.storage.preferences.ps1" `
        "keeps selected branch and typed preference audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorStoragePreferenceChecks",
            "LauncherPreferences.cs",
            "LauncherPreferences.GameBranch.cs",
            "LauncherPreferences.ActionPreferences.cs",
            "LauncherPreferences.BooleanPreference.cs",
            "ReadGameBranch",
            "SaveGameBranch"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.storage.cloud.ps1" `
        "keeps cloud and local-backup preference audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorStorageCloudPreferenceChecks",
            "LauncherPreferences.LocalBackup.cs",
            "LauncherPreferences.CloudSync.cs",
            "SaveLocalBackupEnabled",
            "SaveCloudSyncEnabled"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.download-section.ps1" `
        "keeps install/download selector UI audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorDownloadSectionChecks",
            "DownloadSection.Construction.Version.Dropdown.cs",
            "DownloadSection.Branches.cs",
            "DownloadSection.Branches.Text.cs",
            "LauncherBranchDropdown.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.action-section.ps1" `
        "keeps ready/action selector UI audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorActionSectionChecks",
            "ActionSection.Branches.cs",
            "ActionSection.Branches.Text.cs",
            "ActionSection.Construction.Branch.cs"
        )
}
