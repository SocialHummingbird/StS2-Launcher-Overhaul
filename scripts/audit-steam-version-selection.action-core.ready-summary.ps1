function Add-SteamVersionSelectionActionCoreReadySummaryChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Ready.cs" `
        "configures compact ready-version summary as a readable responsive summary card" `
        @(
            "new Button",
            "TooltipText = `"Open save safety check`"",
            "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
            "ApplyReadyVersionSummaryButtonStyle",
            "readyVersionSummaryPanel\.Pressed \+= OpenCompactCloudSafetyFromReadySummary",
            "ApplyReadyVersionSummaryButtonStyle\(readyVersionSummaryPanel, scale, compact\)",
            "CompactVersionSummaryFontSize",
            "VerticalAlignment\.Center",
            "_compactStackedActionRows\s*\?\s*TextServer\.AutowrapMode\.WordSmart",
            "readyVersionSummaryLabel\.ClipText = compact && !_compactStackedActionRows",
            "readyVersionSummaryLabel\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
            "readyVersionSummaryLabel\.OffsetLeft",
            "readyVersionSummaryLabel\.OffsetRight",
            "TextServer\.OverrunBehavior\.TrimEllipsis",
            "CompactStackedVersionSummaryHeight",
            "CompactVersionSummaryHeight"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "uses responsive compact ready-version copy with Save Check and Upload-locked state" `
        @(
            "CompactReadySummaryBranchLimit",
            "CompactReadyStackedSummaryBranchLimit",
            "CompactReadyVersionSummary\(\)",
            "CompactReadyVersionHelpText\(\)",
            "SelectedOptionCompactStatus",
            "Play version:",
            "Saves: Get/Upload",
            "CompactReadyFileScope",
            "Default files",
            "Separate files",
            "_compactStackedActionRows",
            "Ready:",
            "Save Check \| Upload locked",
            "no auto cloud upload"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.Style.cs" `
        "keeps ready-version summary card skinning isolated from copy generation" `
        @(
            "ApplyReadyVersionSummaryButtonStyle",
            "LauncherComponentTheme\.StateNormal",
            "LauncherComponentTheme\.StateHover",
            "LauncherComponentTheme\.StatePressed",
            "LauncherComponentTheme\.StateDisabled",
            "CompactVersionSummaryRadius",
            "CompactVersionSummaryHorizontalMargin",
            "CompactVersionSummaryVerticalMargin",
            "Color body,",
            "Color border",
            "BuildReadyVersionSummaryStyle\(float scale, bool compact\)",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
            "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
        "opens compact cloud safety details from the ready-version summary" `
        @(
            "OpenCompactCloudSafetyFromReadySummary",
            "_cloudSafetyExpanded = true",
            "UpdateBranchHelpText\(\)"
        )
}
