function Add-SteamVersionSelectionPortalBehaviorCompactGuidanceChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "suppresses compact first-run safe-flow guidance during active auth states" `
        @(
            "SetFirstRunGuideVisible\(false\)",
            "SetLoginFormVisible\(bool visible, bool disabled\)[\s\S]*SetFirstRunGuideVisible\(false\)[\s\S]*HideCompactCompletedAuthSections",
            "ShowCodePrompt\(bool wasIncorrect\)[\s\S]*SetFirstRunGuideVisible\(false\)",
            "SetLoginFormVisible",
            "FirstRunGuide\.Visible = !_profile\.Compact \|\| visible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "suppresses compact first-run safe-flow guidance during active download states" `
        @(
            "ShowDownloadAction\(string buttonText\)[\s\S]*SetFirstRunGuideVisible\(false\)",
            "ShowDownloadAction"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "restores compact setup guidance only when no active action section is shown" `
        @(
            "SetFirstRunGuideVisible\(true\)",
            "SetFirstRunGuideVisible\(false\)",
            "ShowLaunchActions",
            "ShowRetry",
            "HideActions"
        )
}
