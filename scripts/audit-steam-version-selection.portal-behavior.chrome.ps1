function Add-SteamVersionSelectionPortalBehaviorChromeChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.FmodAttribution.cs" `
        "keeps compact FMOD attribution low-profile without expanding the phone layout" `
        @(
            "BuildFmodAttributionSection\(float scale, bool compact\)",
            "Control\.SizeFlags\.ShrinkBegin",
            "Control\.SizeFlags\.ExpandFill",
            "if \(!compact\)",
            "CompactFmodCreditFontSize",
            "AutowrapMode"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
        "uses a framed launcher shell rather than a flat unbounded panel" `
        @(
            "PanelBackground",
            "BorderColor",
            "SetBorderWidthAll",
            "PanelRadius"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.StandaloneLauncher.cs" `
        "suppresses raw startup fallback banner behind the launcher portal" `
        @(
            "Startup fallback raw banner suppressed",
            "launcher diagnostics retain the startup failure detail"
        )
}
