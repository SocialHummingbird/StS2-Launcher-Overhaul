function Add-SteamVersionSelectionActionCoreConstructionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
        "keeps compact action construction ordered as branch details before ready/cloud/support controls" `
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
            "(?s)BuildBranchControls\(scale, compact\).*BuildReadyVersionSummaryControls\(scale, compact\).*SetGameBranch\(_gameBranch\).*BuildCloudControls\(scale, compact\).*BuildSupportControls\(scale, compact, supportToolsParent\).*ArrangeCompactReadyStatePriority\(\)"
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
