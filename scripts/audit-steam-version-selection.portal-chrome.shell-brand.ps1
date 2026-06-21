function Add-SteamVersionSelectionPortalChromeShellBrandChecks {
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
}
