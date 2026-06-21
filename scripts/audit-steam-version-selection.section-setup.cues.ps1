function Add-SteamVersionSelectionSectionSetupCueChecks {
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
