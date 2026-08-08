function Add-SteamVersionSelectionCompactInstallVersionSummaryChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.cs" `
        "builds compact selected-version layout rows and primary-action placement" `
        @(
            "compactStackedActionRows \? new VBoxContainer\(\) : new HBoxContainer\(\)",
            "BuildCompactVersionControlsRow",
            "MoveCompactPrimaryInstallControlsBeforeVersionDetails",
            "MoveChild\(_compactSelectedVersionPanel, _branchDetailsToggle\.GetIndex\(\)\)",
            "MoveChild\(_downloadButton, _branchDetailsToggle\.GetIndex\(\)\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.Summary.cs" `
        "renders compact selected-version summary local-file copy" `
        @(
            "CompactSelectedVersionStackedBranchLimit",
            "CompactSelectedVersionHeadline",
            "Files for:",
            "SelectedOptionCompactStatus",
            "Saves unchanged",
            "Cloud unchanged",
            "Change version",
            "Change",
            "CompactInstallFileScope",
            "Default files",
            "Separate files"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.Summary.Style.cs" `
        "keeps compact selected-version summary card skinning isolated from copy generation" `
        @(
            "ApplySelectedVersionSummaryButtonStyle",
            "LauncherComponentTheme\.StateNormal",
            "LauncherComponentTheme\.StateHover",
            "LauncherComponentTheme\.StatePressed",
            "LauncherComponentTheme\.StateDisabled",
            "BuildSelectedVersionSummaryStyle",
            "CompactVersionSummaryRadius",
            "CompactVersionSummaryHorizontalMargin",
            "CompactVersionSummaryVerticalMargin",
            "Color body,",
            "Color border",
            "BuildSelectedVersionSummaryStyle\(float scale, bool compact\)",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
        )
}
