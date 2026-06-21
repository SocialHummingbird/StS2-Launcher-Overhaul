function Add-SteamVersionSelectionBranchSelectorActionSectionChecks {
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
