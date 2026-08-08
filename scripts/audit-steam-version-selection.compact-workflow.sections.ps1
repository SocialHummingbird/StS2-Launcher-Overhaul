function Add-SteamVersionSelectionCompactWorkflowSectionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "updates compact workflow steps during auth section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Code\)",
            "SetLoginFormVisible",
            "ShowCodePrompt"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "updates compact workflow steps during download section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Files\)",
            "ShowDownloadProgress",
            "SetDownloadProgress"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "updates compact workflow steps during play and retry section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Play\)",
            "ShowLaunchActions",
            "ShowRetry"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "updates compact current-task jump button during auth transitions" `
        @(
            'SetCompactCurrentTask\("Sign in", Login, "Steam account"\)',
            'SetCompactCurrentTask\("Verify", Code, "Steam Guard code"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "updates compact current-task jump button during download transitions" `
        @(
            'SetCompactCurrentTask\("Files", Download, "Download version"\)',
            "ShowDownloadAction",
            "ShowDownloadProgress"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "updates compact current-task jump button during play and retry transitions" `
        @(
            'SetCompactCurrentTask\("Retry", Actions\.RetryScrollTarget, "Restart safely"\)',
            'SetCompactCurrentTask\("Play", Actions\.ReadyScrollTarget, "Play and saves"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "labels compact auth current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Sign in"',
            '"Verify"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "labels compact download current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Files"',
            "Download version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "labels compact play current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Retry"',
            '"Play"'
        )
}
