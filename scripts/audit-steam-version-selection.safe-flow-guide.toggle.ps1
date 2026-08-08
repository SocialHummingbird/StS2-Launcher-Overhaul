function Add-SteamVersionSelectionSafeFlowGuideToggleChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.cs" `
        "builds the collapsible compact quick-start guide toggle shell" `
        @(
            "BuildCollapsedFirstRunGuide",
            "BuildFirstRunGuidePanel\(scale, compact: true\)",
            "toggle\.Pressed \+= \(\) =>",
            "guide\.Visible = !guide\.Visible",
            "`"Quick Start`"",
            "`"Get saves first`"",
            "`"Hide Guide`"",
            "`"Safe order`"",
            "LauncherSectionMetrics\.CompactDrawerToggleHeight",
            "LauncherSectionMetrics\.CompactDetailButtonFontSize",
            "CompactButtonDetailLabelSpec",
            "CompactSafeFlowToggleLabels",
            "CompactSafeFlowToggleBodyName",
            "CompactSafeFlowToggleTitleName",
            "CompactSafeFlowToggleDetailName",
            "CompactButtonDetailLabelSpec\.Default"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.Text.cs" `
        "sets compact quick-start guide toggle text through structured labels" `
        @(
            "SetCompactSafeFlowToggleText",
            "CompactButtonDetailLabels\.Apply",
            '\$"\{title\}\\n\{detail\}"',
            "enabled: true",
            "CompactSafeFlowToggleLabels"
        )
}
