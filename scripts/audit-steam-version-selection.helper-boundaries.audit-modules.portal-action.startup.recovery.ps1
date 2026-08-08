function Add-SteamVersionSelectionPortalActionStartupRecoveryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.startup-recovery.ps1" `
        "keeps startup recovery panel and report audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStartupRecoveryChecks",
            "LauncherDiagnostics.StartupRecoveryReports.cs",
            "LauncherStartupRecoveryControlPanel.Text.cs",
            "LauncherStartupRecoveryControlPanel.Buttons.cs",
            "LauncherStartupRecoveryControlPanel.Layout.cs",
            "LauncherStartupRecoveryControlPanel.Construction.cs"
        )
}
