function Add-SteamVersionSelectionActionCoreConstructionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
        "constructs existing action controls once before assigning stable destinations" `
        @(
            "BuildBranchControls\(scale, compact\)",
            "_branchDetailsToggle = branchControls\.DetailsToggle",
            "_branchDropdown = branchControls\.Dropdown",
            "BuildReadyVersionSummaryControls\(scale, compact\)",
            "SetGameBranch\(_gameBranch\)",
            "BuildCloudControls\(scale, compact\)",
            "_cloudSafetyToggle = cloudControls\.CloudSafetyToggle",
            "_cloudOptionsToggle = cloudControls\.CloudOptionsToggle",
            "BuildSupportControls\(scale, compact, supportToolsParent\)",
            "_supportToggle = supportControls\.SupportToggle",
            "BuildDestinationLayout\(\)",
            "(?s)BuildBranchControls\(scale, compact\).*BuildReadyVersionSummaryControls\(scale, compact\).*SetGameBranch\(_gameBranch\).*BuildCloudControls\(scale, compact\).*BuildModsControls\(scale, compact\).*BuildSupportControls\(scale, compact, supportToolsParent\).*BuildDestinationLayout\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Branch.cs" `
        "uses primary compact touch-target sizing for the ready-state version dropdown" `
        @(
            "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
            "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
            "ApplyDropdownAction",
            "(?s)ApplyDropdownAction\(\s*branchDropdown,\s*scale,.*?,\s*compact\s*\)"
        )
}
