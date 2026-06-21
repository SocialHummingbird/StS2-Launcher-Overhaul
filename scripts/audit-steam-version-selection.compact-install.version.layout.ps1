function Add-SteamVersionSelectionCompactInstallVersionLayoutChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "passes compact width class into the Game Install section for responsive selected-version summaries" `
        @(
            "new DownloadSection\(scale, profile\.Compact, profile\.CompactStackedActionRows\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
        "keeps compact version drawer toggle before expanded install-version controls" `
        @(
            "_branchDetailsToggle = BuildBranchDetailsToggle",
            "AddChild\(_branchDetailsToggle\)",
            "_branchDropdown = BuildBranchDropdown",
            "(?s)_branchDetailsToggle = BuildBranchDetailsToggle.*AddChild\(_branchDetailsToggle\).*_branchDropdown = BuildBranchDropdown"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
        "uses primary compact touch-target sizing for the install-version dropdown" `
        @(
            "BuildBranchDropdown",
            "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
            "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
            "ApplyDropdownAction",
            "(?s)ApplyDropdownAction\(\s*dropdown,\s*scale,.*?,\s*compact\s*\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.cs" `
        "puts the compact install primary action before optional version details" `
        @(
            "MoveCompactPrimaryInstallControlsBeforeVersionDetails",
            "MoveChild\(_compactSelectedVersionPanel, _branchDetailsToggle\.GetIndex\(\)\)",
            "MoveChild\(_downloadButton, _branchDetailsToggle\.GetIndex\(\)\)"
        )
}
