function Add-SteamVersionSelectionSectionSetupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.cs" `
        "frames hidden launcher states through a small section setup entrypoint" `
        @(
            "ConfigureHiddenSection",
            "internal static partial class LauncherSectionSetup",
            "bool compact",
            "compactCue",
            "accent",
            "LauncherSectionMetrics\.CompactSectionSeparation",
            "LauncherSectionMetrics\.SectionSeparation",
            "section\.Visible = false",
            "BuildSectionHeader\(title, subtitle, scale, accent, compact, compactCue\)"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
        "uses explicit compact Steam sign-in section cue text" `
        @(
            "Steam Sign-in",
            "Steam account"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
        "uses explicit compact Steam Guard section cue text" `
        @(
            "Steam Guard",
            "Current code"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
        "uses explicit compact game install section cue text" `
        @(
            "Game Install",
            "Local files"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
        "uses explicit compact play and sync section cue text" `
        @(
            "Play and Sync",
            "Play safely"
        )

}
