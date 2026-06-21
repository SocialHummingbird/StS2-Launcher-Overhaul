function Add-SteamVersionSelectionSupportDocsDiagnosticsBranchSwitchBoundaryChecks {

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.ps1" `
        "keeps branch-switch safety diagnostics audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchChecks",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.shell.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.marker.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.pull.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.save-origin.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.push.ps1",
            "audit-steam-version-selection.diagnostics-reporting.branch-switch.backup.ps1",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchShellChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchMarkerChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPullChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchSaveOriginChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPushChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchBackupChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.shell.ps1" `
        "keeps branch-switch safety diagnostics orchestration checks focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchShellChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.cs",
            "AppendBranchSwitchSafety",
            "AppendManualPushBlockedEvidence"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.marker.ps1" `
        "keeps branch-switch marker diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchMarkerChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Marker.cs",
            "Branch switch marker has required safety evidence for selected branch"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.pull.ps1" `
        "keeps branch-switch Manual Pull diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPullChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Pull.cs",
            "Manual Pull completed after branch switch for selected version"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.save-origin.ps1" `
        "keeps branch-switch save-origin diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchSaveOriginChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs",
            "Android local saves verified for selected runtime"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.push.ps1" `
        "keeps branch-switch Manual Push and blocked-Push diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPushChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs",
            "LauncherDiagnostics.ReportBranchSwitchSafety.BlockedPush.cs",
            "Manual Push blocked before upload evidence recorded"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.branch-switch.backup.ps1" `
        "keeps branch-switch backup diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchBackupChecks",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Backup.cs",
            "Branch-switch pre-Push backup evidence satisfied"
        )
}
