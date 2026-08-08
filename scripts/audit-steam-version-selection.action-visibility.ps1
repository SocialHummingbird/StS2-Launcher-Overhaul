function Add-SteamVersionSelectionActionVisibilityChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
        "promotes compact retry recovery to a primary structured action" `
        @(
            'compact \? CompactRetryButtonText\(\) : "RETRY"',
            "LauncherButtonStyles\.ApplyPrimaryAction\(retryButton, scale\)",
            "SetCompactActionButtonText\(retryButton, retryButton\.Text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "labels compact retry recovery as TRY AGAIN with restart-task detail" `
        @(
            "CompactRetryButtonText",
            'CompactPlaySyncDrawerText\("Try Again", "Restart task"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "labels compact launch CTA with selected-version detail" `
        @(
            "CompactLaunchButtonText\(string text\)",
            "CompactLaunchButtonText",
            "Start Game",
            "Ready version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "applies compact launch CTA text to the launch button" `
        @(
            "SetCompactActionButtonText\(_launchButton",
            "CompactLaunchButtonText\(text\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "reveals the framed play/sync action section when launch or retry actions are available" `
        @(
            "internal void ShowLaunch",
            "internal void ShowRetry",
            "internal void HideAll",
            "Visible = true",
            "Visible = false",
            "SetCloudControlsVisible",
            "ShowLaunchButtons",
            "ShowRetryButtons",
            "HideSecondaryButtons"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.SecondaryState.cs" `
        "models ready, retry, and hidden secondary button visibility presets" `
        @(
            "SecondaryButtonVisibility",
            "LaunchReady\(bool showUpdate\)",
            "Retry\(\)",
            "Hidden\(\)",
            "redownload: true",
            "support: true",
            "safeLaunch: true",
            "launch: true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Secondary.cs" `
        "applies secondary ready/retry/hidden button visibility to the action section" `
        @(
            "ShowLaunchButtons",
            "ShowRetryButtons",
            "HideSecondaryButtons",
            "SetSecondaryButtonsVisible",
            "ShowUpdateButton\(visibility\.Update\)",
            "_redownloadButton\.Visible = visibility\.Redownload",
            "_branchControlsAvailable = visibility\.Branch",
            "ApplyBranchControlVisibility",
            "SetSupportButtonsVisible\(visibility\.Support\)",
            "_readyVersionSummaryPanel\.Visible = _compact && visibility\.Launch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Support.cs" `
        "keeps Help tools available while Versions tools follow action readiness" `
        @(
            "ShowUpdateButton",
            "CompactSupportToolText\(""Check Files"", ""Updates""\)",
            "Check for Updates",
            "SetSupportButtonsVisible",
            "_supportToggle\.Visible = false",
            "_supportExpanded = false",
            "_supportGroup\.Visible = false",
            "_diagnosticsButton\.Visible = true",
            "_refreshVersionsButton\.Visible = visible",
            "_clearCachedVersionsButton\.Visible = visible",
            "_showLastErrorButton\.Visible = true",
            "_copyRawLogButton\.Visible = true"
        )
}
