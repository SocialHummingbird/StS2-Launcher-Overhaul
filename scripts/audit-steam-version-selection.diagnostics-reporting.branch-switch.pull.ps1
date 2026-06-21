function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPullChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Pull.cs" `
        "reports Manual Pull evidence for branch-switch Push safety" `
        @(
            "AppendManualPullEvidence",
            "Manual Pull evidence marker filename",
            "Manual Pull evidence marker path",
            "Manual Pull evidence marker present",
            "Manual Pull evidence UTC",
            "Manual Pull evidence UTC parseable",
            "Manual Pull evidence selected branch",
            "Manual Pull evidence selected branch selection kind",
            "Manual Pull evidence selector mode",
            "Manual Pull evidence selected version",
            "Manual Pull evidence selected version slot kind",
            "Manual Pull evidence selected version slot directory",
            "Manual Pull completion flag recorded",
            "Manual Pull completed before Push",
            "Manual Pull evidence is after branch switch",
            "Manual Pull evidence matches selected branch",
            "Manual Pull completed after branch switch for selected version"
        )
}
