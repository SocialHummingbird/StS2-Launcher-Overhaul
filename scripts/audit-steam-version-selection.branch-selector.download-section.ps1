function Add-SteamVersionSelectionBranchSelectorDownloadSectionChecks {
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
}
