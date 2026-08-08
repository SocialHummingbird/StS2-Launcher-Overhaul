function Add-SteamVersionSelectionSectionSetupHeaderChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.cs" `
        "builds desktop launcher section headers separately from compact mobile headers" `
        @(
            "BuildSectionHeader",
            "if \(compact\)",
            "BuildCompactSectionHeader\(title, CompactCueText\(compactCue, subtitle\), subtitle, scale, accent\)",
            "new PanelContainer",
            "new VBoxContainer",
            "BuildHeaderStyle\(scale, compact\)",
            "BuildDesktopSectionAccent",
            "BuildDesktopSectionTitle",
            "AddDesktopSectionSubtitle",
            "fontSize: 13",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "LauncherComponentTheme\.TextSecondary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.Compact.cs" `
        "keeps compact launcher section headers readable and fixed-size on mobile" `
        @(
            "BuildCompactSectionHeader",
            "CompactSectionHeaderMinHeight = 42",
            "CompactSectionHeaderCueFontSize = 12",
            "CompactSectionHeaderTitleFontSize = 14",
            "CompactSectionHeaderTitleMinWidth = 106",
            "CompactSectionHeaderAccentWidth = 3",
            "new HBoxContainer",
            "BuildCompactSectionAccent",
            "BuildCompactSectionTitle",
            "BuildCompactSectionCue",
            "CompactCueText",
            "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "TooltipText = tooltip",
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.Style.cs" `
        "isolates section header style metrics for compact and desktop layouts" `
        @(
            "BuildHeaderStyle\(float scale, bool compact\)",
            "LauncherStyleBoxes\.MakeFilled",
            "compact \? 6 : 8",
            "SetBorderWidthAll",
            "compact \? 7 : 10",
            "compact \? 4 : 8",
            "compact \? 4 : 9"
        )
}
