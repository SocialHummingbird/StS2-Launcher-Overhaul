function Add-SteamVersionSelectionSupportDocsDiagnosticsReportingBoundaryChecks {

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.ps1" `
        "keeps launcher diagnostics report and branch-switch evidence audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingChecks",
            "audit-steam-version-selection.diagnostics-reporting.shell.ps1",
            "audit-steam-version-selection.diagnostics-reporting.launcher-state.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.ps1",
            "Add-SteamVersionSelectionDiagnosticsReportingShellChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingLauncherStateChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.shell.ps1" `
        "keeps diagnostics public-sharing shell audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingShellChecks",
            "LauncherDiagnosticsCoordinator.cs",
            "LauncherStartupRecoveryControlPanel.Reports.cs",
            "LauncherDiagnostics.Reports.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.launcher-state.ps1" `
        "keeps launcher preference, branch availability, and cache diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingLauncherStateChecks",
            "audit-steam-version-selection.diagnostics-reporting.launcher-state.preferences.ps1",
            "audit-steam-version-selection.diagnostics-reporting.launcher-state.branch-availability.ps1",
            "audit-steam-version-selection.diagnostics-reporting.launcher-state.cached-versions.ps1",
            "Add-SteamVersionSelectionDiagnosticsReportingLauncherPreferenceChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchAvailabilityChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingCachedVersionChecks"
        )
}
