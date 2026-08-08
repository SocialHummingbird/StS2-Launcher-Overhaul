function Add-SteamVersionSelectionCompactInstallVersionShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
        "centralizes compact install control sizing constants and width class state" `
        @(
            "CompactDownloadActionBodyName",
            "CompactDownloadActionTitleName",
            "CompactDownloadActionDetailName",
            "CompactDownloadActionHeight = LauncherSectionMetrics\.CodeInputHeight",
            "CompactVersionHelpHeight",
            "CompactVersionHelpFontSize",
            "_compactSelectedVersionLabel",
            "_compactSelectedVersionPanel",
            "_compactVersionControlsRow",
            "BuildCompactVersionControlsRow",
            "CompactSelectedVersionBranchLimit = 18",
            "CompactSelectedVersionStackedBranchLimit = 28",
            "_compactStackedActionRows = compact && compactStackedActionRows",
            "compactStackedActionRows = false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.cs" `
        "constructs the compact install version details toggle" `
        @(
            "BuildBranchDetailsToggle",
            "Show Version Details",
            "LauncherSectionMetrics\.CompactDrawerToggleHeight",
            "LauncherButtonStyles\.ApplySupportAction",
            "button\.Pressed \+= ToggleBranchDetails"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Selected.cs" `
        "constructs compact selected-version summary controls with local-file cues" `
        @(
            "BuildCompactSelectedVersionPanel",
            "BuildCompactSelectedVersionLabel",
            "ApplySelectedVersionSummaryButtonStyle",
            "button\.Pressed \+= OpenCompactBranchDetailsFromSelectedVersion",
            "TooltipText = `"Change game version for local files`"",
            "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
            "CompactVersionSummaryFontSize",
            "CompactVersionSummaryHeight",
            "CompactStackedVersionSummaryHeight",
            "CompactVersionSummaryHorizontalMargin",
            "CompactVersionSummaryVerticalMargin",
            "label\.ClipText = compact",
            "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "label\.CustomMinimumSize = new Vector2",
            "AutowrapMode\.Off",
            "TextServer\.AutowrapMode\.WordSmart",
            "TextServer\.OverrunBehavior\.TrimEllipsis",
            "ClipText = compact && !_compactStackedActionRows"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
        "constructs and places the install-version dropdown in compact and full layouts" `
        @(
            "BuildBranchDropdown",
            "_compactVersionControlsRow\.AddChild\(_branchDropdown\)",
            "AddChild\(_compactVersionControlsRow\)",
            "AddChild\(_branchDropdown\)",
            "dropdown\.ItemSelected \+= ApplyGameBranch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Refresh.cs" `
        "constructs refresh-version and bounded branch help controls" `
        @(
            "BuildRefreshBranchesButton",
            "_compactVersionControlsRow\.AddChild\(_refreshBranchesButton\)",
            "RefreshGameVersionsRequested\?\.Invoke",
            "SetCompactVersionActionButtonText",
            "Refresh Versions",
            "Update branch list",
            "BuildBranchHelpLabel",
            "CompactVersionHelpFontSize",
            "CompactVersionHelpHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.Text.cs" `
        "states that version downloads affect local files and do not mutate Steam Cloud saves" `
        @(
            "_branchDetailsExpanded",
            "Download/update changes local files for the selected game version only",
            "does not change Steam Cloud saves",
            "SelectedOptionStatus",
            "SelectorInstallSlotHelpText",
            "CompactInstallVersionHelpText",
            "Selected version:",
            "Install slot:",
            "Downloads do not change Steam Cloud saves",
            "SetCompactVersionActionButtonText",
            "ApplyBranchControlVisibility\(\)"
        )
}
