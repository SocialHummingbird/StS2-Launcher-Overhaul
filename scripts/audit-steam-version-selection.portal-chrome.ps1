function Add-SteamVersionSelectionPortalChromeChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.cs" `
        "keeps launcher shell rooted as a polished portal layout" `
        @(
            "BuildShell",
            "StyledPanel",
            "ScreenBackground",
            "BuildBrandHeader\(profile\)",
            "CompactRootColumnSeparation",
            "RootColumnSeparation",
            "panel\.AddContent\(content\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.cs" `
        "keeps brand-header entry point split between compact and desktop layouts" `
        @(
            "CompactBrandTitleFontSize = 18",
            "CompactBrandSubtitleFontSize = 12",
            "CompactBrandRowSeparation = 6",
            "CompactBrandHeaderSeparation = 2",
            "BuildCompactBrandHeader",
            "BuildDesktopBrandCopy",
            "BuildBrandDivider",
            "profile\.Compact",
            "if \(profile\.Compact\)",
            "return BuildCompactBrandHeader\(profile\)",
            "BuildBrandMark\(scale, compact: false\)",
            "BuildBrandDivider\(scale, height: 2\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.Desktop.cs" `
        "presents desktop launcher brand copy as a polished Steam/version/cloud portal" `
        @(
            "BuildDesktopBrandCopy",
            "StS2 Mobile",
            "Sign in\. Save safely\. Play\.",
            "fontSize: 26",
            "fontSize: 11",
            "HorizontalAlignment\.Left",
            "LauncherComponentTheme\.TextPrimary",
            "LauncherComponentTheme\.CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.Compact.cs" `
        "presents compact launcher brand copy in one condensed mobile row" `
        @(
            "BuildCompactBrandHeader",
            "CompactBrandHeaderSeparation",
            "CompactBrandRowSeparation",
            "BuildBrandMark\(scale, compact: true\)",
            "BuildCompactBrandTitle",
            "BuildCompactBrandSubtitle",
            "BuildBrandDivider\(scale, height: 1\)",
            "StS2 Mobile",
            "Saves safe\. Ready to play\.",
            "fontSize: CompactBrandTitleFontSize",
            "fontSize: CompactBrandSubtitleFontSize",
            "title\.ClipText = true",
            "subtitle\.ClipText = true",
            "CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.BrandMark.cs" `
        "renders compact and desktop launcher brand mark consistently" `
        @(
            "CompactBrandMarkHeight = 26",
            "BuildBrandMark",
            "BuildBrandMarkStripe",
            "compact \? CompactBrandMarkHeight : 50",
            "compact \? 12 : 16",
            "OrangeAccent",
            "CyanAccent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLayoutProfile.cs" `
        "uses the full available Android viewport for compact launcher layouts" `
        @(
            "mobileShell = OperatingSystem\.IsAndroid\(\)",
            "compact = mobileShell",
            "AndroidCompactTouchScaleFloor = 1\.06f",
            "mobileShell \? AndroidCompactTouchScaleFloor : CompactScaleFloor",
            "CompactStackedActionRowsWidth = 560f",
            "CompactStackedActionRows",
            "contentMaxWidth < MathF\.Round\(CompactStackedActionRowsWidth \* scale\)",
            "panelWidth = compact \? 1\.0f",
            "panelHeight = compact \? 1\.0f",
            "Math\.Min\(safeViewport\.X \* 0\.96f, 1600f\)",
            "Math\.Min\(safeViewport\.X \* 0\.84f, 1180f\)",
            "CompactStackedActionRows=\{CompactStackedActionRows\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
        "reduces compact-mode panel padding and avoids a short fixed-height phone panel" `
        @(
            "MaxHeight = 2200f",
            "CompactPanelHorizontalMargin = 10",
            "CompactPanelTopMargin = 10",
            "CompactPanelBottomMargin = 12",
            "compact \? CompactPanelHorizontalMargin : LauncherComponentTheme\.PanelHorizontalMargin",
            "compact \? CompactPanelTopMargin : LauncherComponentTheme\.PanelTopMargin",
            "compact \? CompactPanelBottomMargin : LauncherComponentTheme\.PanelBottomMargin",
            "_compact\s*\?\s*vpSize\.Y \* heightRatio",
            "BuildStyle\(scale, compact\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Support.cs" `
        "adds compact-only bottom scroll breathing room for phone gesture areas" `
        @(
            "BuildCompactBottomScrollSpacer",
            "LauncherViewLayoutMetrics\.ScaleInt\(72, scale\)",
            "MouseFilterEnum\.Ignore"
        )

}
