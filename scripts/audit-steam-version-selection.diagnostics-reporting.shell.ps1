function Add-SteamVersionSelectionDiagnosticsReportingShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnosticsCoordinator.cs" `
        "warns before launcher logs are copied for sharing" `
        @(
            "Public sharing warning",
            "review and redact this launcher log before posting publicly",
            "Review/redact before public posting",
            "Launcher log copied"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Reports.cs" `
        "warns after startup recovery launcher logs are copied" `
        @(
            "Launcher log copied to clipboard",
            "Review/redact before public posting"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
        "keeps the diagnostics report shell and public-sharing warning" `
        @(
            "Public sharing warning",
            "review and redact this diagnostics report before posting publicly",
            "AppendLauncherPreferences",
            "AppendFullReportDiagnostics"
        )
}
