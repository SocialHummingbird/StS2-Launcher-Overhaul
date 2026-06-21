function Add-SteamVersionSelectionCompactInstallChecks {
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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.ActionButton.cs" `
        "renders compact version action labels as structured title/detail controls" `
        @(
            "SetCompactVersionActionButtonText",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactVersionActionLabels",
            "CompactVersionActionBodyName",
            "CompactVersionActionTitleName",
            "CompactVersionActionDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "enabled: false",
            "enabled: true",
            '\$"\{title\}\\n\{detail\}"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "maps compact download action copy to local-file-only title/detail labels" `
        @(
            "CompactDownloadButtonText",
            "CompactDownloadButtonTitleDetail",
            "`"Download Version`"",
            "`"Redownload Version`"",
            "`"Retry Download`"",
            "`"Downloading\.\.\.`"",
            "Local files only",
            "Rebuild local files",
            "Steam files"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.ActionButton.cs" `
        "renders compact install primary actions as structured title/detail labels" `
        @(
            "CompactDownloadActionLabels",
            "CompactButtonDetailLabelSpec",
            "CompactDownloadActionBodyName",
            "CompactDownloadActionTitleName",
            "CompactDownloadActionDetailName",
            "CompactDownloadActionTitleFontSize",
            "CompactDownloadActionDetailFontSize",
            "CompactDownloadActionHorizontalMargin",
            "CompactDownloadActionVerticalMargin",
            "SetCompactDownloadButtonText",
            "CompactButtonDetailLabels\.Apply"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "keeps compact install primary-action copy as structured title/detail text" `
        @(
            "CompactDownloadButtonTitleDetail",
            "CompactDownloadButtonText",
            "`"DOWNLOAD SELECTED VERSION`"",
            "`"Download Version`"",
            "`"Local files only`"",
            "`"REDOWNLOAD SELECTED VERSION`"",
            "`"Redownload Version`"",
            "`"Rebuild local files`"",
            "`"Retry Download`"",
            "`"Downloading\.\.\.`"",
            "`"Steam files`""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.cs" `
        "promotes compact download progress controls directly under the active primary action" `
        @(
            "MoveCompactProgressControlsNearPrimaryAction",
            "MoveChild\(_progressLabel, _downloadButton\.GetIndex\(\) \+ 1\)",
            "MoveChild\(_progressBar, _progressLabel\.GetIndex\(\) \+ 1\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
        "keeps compact download progress copy concise and bounded" `
        @(
            "CompactDownloadProgressButtonText",
            "CompactDownloadProgressText",
            "CompactDownloadProgressDetail",
            "NormalizeCompactProgressText",
            "Downloading selected version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Download.cs" `
        "constructs compact download progress controls with readable mobile sizing" `
        @(
            "new StyledProgressBar\(scale, compact\)",
            "BuildProgressLabel",
            "label\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "label\.ClipText = compact",
            "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "label\.CustomMinimumSize = new Vector2",
            "compact\s*\?\s*LauncherSectionMetrics\.SecondaryButtonFontSize",
            "compact\s*\?\s*LauncherComponentTheme\.CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.Progress.cs" `
        "updates compact download progress state next to the disabled primary action" `
        @(
            "_progressLabel\.Text = _compact \? CompactDownloadProgressText\(text\) : text",
            "SetCompactDownloadButtonText\(_downloadButton, CompactDownloadProgressButtonText\(\)\)",
            "_compactSelectedVersionPanel\.Disabled = true",
            "_compactSelectedVersionPanel\.Disabled = false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
        "defines compact readable download progress bar metrics and colors" `
        @(
            "ProgressBarHeight = 24",
            "CompactProgressBarHeight = 34",
            "ProgressBarFontSize = 12",
            "CompactProgressBarFontSize = 14",
            "ProgressBarRadius = 6",
            "ProgressBackground",
            "ProgressFill",
            "ProgressFillCompact"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
        "rounds shared component scaling instead of flooring compact Android metrics" `
        @(
            "using System;",
            "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
            "Math\.Max\(0,"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherViewLayoutMetrics.cs" `
        "rounds shared layout scaling instead of flooring compact Android metrics" `
        @(
            "using System;",
            "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
            "Math\.Max\(0,"
        )
}
