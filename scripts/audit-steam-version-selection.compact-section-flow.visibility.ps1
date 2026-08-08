function Add-SteamVersionSelectionCompactSectionFlowVisibilityChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "restores compact install section for downloads" `
        @(
            "ShowDownloadAction",
            "SetCompactReadyInstallSectionVisible\(true\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "suppresses compact install section after launch-ready" `
        @(
            "SetCompactReadyInstallSectionVisible",
            "ShowLaunchActions",
            "SetCompactReadyInstallSectionVisible\(false\)",
            "!_profile\.Compact",
            "Download\.Visible = visible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "defines completed compact auth section suppression" `
        @(
            "HideCompactCompletedAuthSections",
            "ShowCodePrompt",
            "HideCompactCompletedAuthSections\(showCode: true\)",
            "Login\.SetFormVisible\(false, disabled: true\)",
            "Code\.Visible = showCode"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "suppresses completed compact auth sections before download work" `
        @(
            "ShowDownloadAction",
            "HideCompactCompletedAuthSections\(showCode: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "suppresses completed compact auth sections before play or retry work" `
        @(
            "ShowRetry",
            "ShowLaunchActions",
            "HideCompactCompletedAuthSections\(showCode: false\)"
        )
}
